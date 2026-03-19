#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
# ChimQuiz — Déploiement Azure Container Apps + Cosmos DB
#
# Usage:
#   ./deploy.sh                      # déploiement complet (infra + domaine)
#   ./deploy.sh --infra-only         # déploie sans binder le domaine
#   ./deploy.sh --domain-only        # bind le domaine sur une infra existante
#   ./deploy.sh --update-image       # redémarre avec la dernière image
#
# Pré-requis:
#   - az CLI installé et connecté (az login)
#   - Extension containerapp: az extension add --name containerapp
# ─────────────────────────────────────────────────────────────────────────────
set -euo pipefail

# ── Configuration ─────────────────────────────────────────────────────────────
RESOURCE_GROUP="rg-chimquiz"
LOCATION="westeurope"
CUSTOM_DOMAIN="chimquiz.bl0m.art"
APP_NAME="chimquiz"
TEMPLATE_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DEPLOYMENT_NAME="chimquiz-$(date +%Y%m%d%H%M%S)"

# ── Couleurs ──────────────────────────────────────────────────────────────────
RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'
CYAN='\033[0;36m'; BOLD='\033[1m'; NC='\033[0m'
info()    { echo -e "${CYAN}ℹ️  $*${NC}"; }
success() { echo -e "${GREEN}✅ $*${NC}"; }
warn()    { echo -e "${YELLOW}⚠️  $*${NC}"; }
error()   { echo -e "${RED}❌ $*${NC}"; exit 1; }

# ── Vérifications pré-vol ──────────────────────────────────────────────────
check_prerequisites() {
  info "Vérification des pré-requis..."
  command -v az &>/dev/null || error "az CLI non trouvé. Installe-le: https://aka.ms/installazureclimacos"
  az account show &>/dev/null  || error "Non connecté à Azure. Lance: az login"
  az extension show --name containerapp &>/dev/null \
    || { warn "Extension containerapp manquante — installation..."; az extension add --name containerapp --yes; }
  success "Pré-requis OK (compte: $(az account show --query name -o tsv))"
}

# ── 1. Resource Group ─────────────────────────────────────────────────────────
deploy_resource_group() {
  info "Resource group: $RESOURCE_GROUP ($LOCATION)"
  az group create \
    --name "$RESOURCE_GROUP" \
    --location "$LOCATION" \
    --output none
  success "Resource group prêt"
}

# ── 2. ARM template (Cosmos DB + Container Apps) ──────────────────────────────
deploy_infrastructure() {
  info "Déploiement ARM (Cosmos DB + Container Apps)..."

  az deployment group create \
    --name "$DEPLOYMENT_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --template-file "$TEMPLATE_DIR/azuredeploy.json" \
    --parameters "@$TEMPLATE_DIR/azuredeploy.parameters.json" \
    --no-wait \
    --output none

  info "Déploiement en cours (peut prendre 3-5 min)..."
  while true; do
    STATUS=$(az deployment group show \
      --name "$DEPLOYMENT_NAME" \
      --resource-group "$RESOURCE_GROUP" \
      --query "properties.provisioningState" \
      --output tsv 2>/dev/null || echo "Running")
    case "$STATUS" in
      Succeeded) break ;;
      Failed|Canceled) error "Déploiement échoué (état: $STATUS). Lance: az deployment group show -n $DEPLOYMENT_NAME -g $RESOURCE_GROUP" ;;
      *) printf "."; sleep 15 ;;
    esac
  done
  echo ""

  APP_FQDN=$(az deployment group show \
    --name "$DEPLOYMENT_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --query "properties.outputs.appFqdn.value" \
    --output tsv)
  APP_URL="https://$APP_FQDN"

  COSMOS_ENDPOINT=$(az deployment group show \
    --name "$DEPLOYMENT_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --query "properties.outputs.cosmosEndpoint.value" \
    --output tsv)

  success "Déploiement terminé"
  echo -e "${BOLD}  URL Azure:      $APP_URL${NC}"
  echo -e "${BOLD}  Cosmos DB:      $COSMOS_ENDPOINT${NC}"
}

# ── 3. Domaine personnalisé + certificat managé gratuit ───────────────────────
bind_custom_domain() {
  info "Configuration du domaine $CUSTOM_DOMAIN..."

  if [[ -z "${APP_FQDN:-}" ]]; then
    APP_FQDN=$(az containerapp show \
      --name "$APP_NAME" \
      --resource-group "$RESOURCE_GROUP" \
      --query "properties.configuration.ingress.fqdn" \
      --output tsv)
  fi

  echo ""
  echo -e "${BOLD}┌─────────────────────────────────────────────────────┐${NC}"
  echo -e "${BOLD}│          Configuration DNS requise                  │${NC}"
  echo -e "${BOLD}├─────────────────────────────────────────────────────┤${NC}"
  echo -e "${BOLD}│  Ajoute ces enregistrements dans ton DNS bl0m.art : │${NC}"
  echo -e "${BOLD}│                                                     │${NC}"
  printf  "${BOLD}│  %-10s  CNAME  %-32s│${NC}\n" "chimquiz" "$APP_FQDN"
  echo -e "${BOLD}│  asuid.chimquiz  TXT    <valeur de validation>      │${NC}"
  echo -e "${BOLD}│                                                     │${NC}"
  echo -e "${BOLD}│  Attends la propagation (1-5 min) puis confirme.   │${NC}"
  echo -e "${BOLD}└─────────────────────────────────────────────────────┘${NC}"
  echo ""

  while true; do
    read -rp "$(echo -e "${YELLOW}DNS propagé ? (o/n) : ${NC}")" answer
    case "$answer" in
      [oOyY]*) break ;;
      [nN]*)
        info "Vérifie avec: dig $CUSTOM_DOMAIN CNAME +short"
        ;;
      *) warn "Réponds par o ou n" ;;
    esac
  done

  info "Ajout du hostname $CUSTOM_DOMAIN..."
  az containerapp hostname add \
    --name "$APP_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --hostname "$CUSTOM_DOMAIN" \
    --output none

  info "Émission du certificat managé gratuit (peut prendre ~2 min)..."
  az containerapp hostname bind \
    --name "$APP_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --hostname "$CUSTOM_DOMAIN" \
    --validation-method CNAME \
    --environment "env-$APP_NAME" \
    --output none

  success "Domaine et certificat configurés"
  echo -e "${BOLD}  URL finale: https://$CUSTOM_DOMAIN${NC}"
}

# ── 4. Mise à jour de l'image ─────────────────────────────────────────────────
update_image() {
  info "Mise à jour vers la dernière image..."
  IMAGE=$(az containerapp show \
    --name "$APP_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --query "properties.template.containers[0].image" \
    --output tsv)
  info "Image actuelle: $IMAGE"

  az containerapp update \
    --name "$APP_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --image "ghcr.io/cblomart/chimquiz:latest" \
    --output none

  success "Image mise à jour et redémarrage lancé"
}

# ── Résumé final ──────────────────────────────────────────────────────────────
print_summary() {
  echo ""
  echo -e "${GREEN}${BOLD}═══════════════════════════════════════════════${NC}"
  echo -e "${GREEN}${BOLD}  ChimQuiz déployé avec succès sur Azure!       ${NC}"
  echo -e "${GREEN}${BOLD}═══════════════════════════════════════════════${NC}"
  echo -e "  ${BOLD}App:${NC}           https://$CUSTOM_DOMAIN"
  echo -e "  ${BOLD}Resource group:${NC} $RESOURCE_GROUP"
  echo -e "  ${BOLD}Région:${NC}        $LOCATION"
  echo -e "  ${BOLD}Base de données:${NC} Cosmos DB Serverless (~0 €/mois)"
  echo -e "  ${BOLD}Scale-to-zero:${NC} minReplicas=0 (économique)"
  echo ""
  echo -e "  Pour mettre à jour l'image: ${CYAN}./deploy.sh --update-image${NC}"
  echo ""
}

# ── Point d'entrée ────────────────────────────────────────────────────────────
MODE="${1:-}"

case "$MODE" in
  --infra-only)
    check_prerequisites
    deploy_resource_group
    deploy_infrastructure
    warn "Domaine non configuré. Lance ./deploy.sh --domain-only quand prêt."
    ;;
  --domain-only)
    check_prerequisites
    bind_custom_domain
    ;;
  --update-image)
    check_prerequisites
    update_image
    ;;
  "")
    check_prerequisites
    deploy_resource_group
    deploy_infrastructure
    bind_custom_domain
    print_summary
    ;;
  *)
    echo "Usage: $0 [--infra-only | --domain-only | --update-image]"
    exit 1
    ;;
esac
