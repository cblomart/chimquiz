#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
# ChimQuiz — Déploiement Azure Container Apps
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
CUSTOM_DOMAIN="chimquiz.blom.art"
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

# ── 2. ARM template (storage + env + container app) ───────────────────────────
deploy_infrastructure() {
  info "Déploiement ARM (storage + Container Apps)..."
  OUTPUTS=$(az deployment group create \
    --name "$DEPLOYMENT_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --template-file "$TEMPLATE_DIR/azuredeploy.json" \
    --parameters "@$TEMPLATE_DIR/azuredeploy.parameters.json" \
    --query "properties.outputs" \
    --output json)

  APP_FQDN=$(echo "$OUTPUTS" | az config get --query "appFqdn.value" 2>/dev/null \
    || echo "$OUTPUTS" | python3 -c "import sys,json; print(json.load(sys.stdin)['appFqdn']['value'])")
  APP_URL="https://$APP_FQDN"

  success "Déploiement terminé"
  echo -e "${BOLD}  URL Azure:   $APP_URL${NC}"
}

# ── 3. Domaine personnalisé + certificat managé gratuit ───────────────────────
bind_custom_domain() {
  info "Configuration du domaine $CUSTOM_DOMAIN..."

  # Récupère le FQDN si pas déjà en mémoire
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
  echo -e "${BOLD}│  Ajoute ce CNAME dans ton DNS blom.art :            │${NC}"
  echo -e "${BOLD}│                                                     │${NC}"
  printf  "${BOLD}│  %-10s  CNAME  %-32s│${NC}\n" "chimquiz" "$APP_FQDN"
  echo -e "${BOLD}│                                                     │${NC}"
  echo -e "${BOLD}│  Attends la propagation (1-5 min) puis confirme.   │${NC}"
  echo -e "${BOLD}└─────────────────────────────────────────────────────┘${NC}"
  echo ""

  # Attente confirmation propagation DNS
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

  # Bind le hostname
  info "Ajout du hostname $CUSTOM_DOMAIN..."
  az containerapp hostname add \
    --name "$APP_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --hostname "$CUSTOM_DOMAIN" \
    --output none

  # Certificat managé gratuit (DigiCert, auto-renouvelé)
  info "Émission du certificat managé gratuit (peut prendre ~2 min)..."
  az containerapp hostname bind \
    --name "$APP_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --hostname "$CUSTOM_DOMAIN" \
    --validation-method CNAME \
    --certificate-type managed \
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
  echo -e "  ${BOLD}Scale-to-zero:${NC} minReplicas=0 (économique)"
  echo -e "  ${BOLD}Coût estimé:${NC}   ~0–1 €/mois (faible trafic)"
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
