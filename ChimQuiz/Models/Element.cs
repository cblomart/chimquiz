namespace ChimQuiz.Models
{
    public record Element(
        int AtomicNumber,
        string Symbol,
        string Name,
        decimal AtomicMass,
        string CommonUse = "",
        string WhereToFind = "",
        string? FunFact = null
    );

    public static class ElementData
    {
        // ── Base data (numéro;symbole;nom;masse) ──────────────────────────────────
        private const string CsvData = @"1;H;Hydrogène;1.008
2;He;Hélium;4.0026
3;Li;Lithium;6.94
4;Be;Béryllium;9.0122
5;B;Bore;10.81
6;C;Carbone;12.011
7;N;Azote;14.007
8;O;Oxygène;15.999
9;F;Fluor;18.998
10;Ne;Néon;20.18
11;Na;Sodium;22.99
12;Mg;Magnésium;24.305
13;Al;Aluminium;26.982
14;Si;Silicium;28.085
15;P;Phosphore;30.974
16;S;Soufre;32.06
17;Cl;Chlore;35.45
18;Ar;Argon;39.948
19;K;Potassium;39.098
20;Ca;Calcium;40.078
21;Sc;Scandium;44.956
22;Ti;Titane;47.867
23;V;Vanadium;50.942
24;Cr;Chrome;51.996
25;Mn;Manganèse;54.938
26;Fe;Fer;55.845
27;Co;Cobalt;58.933
28;Ni;Nickel;58.693
29;Cu;Cuivre;63.546
30;Zn;Zinc;65.38
31;Ga;Gallium;69.723
32;Ge;Germanium;72.63
33;As;Arsenic;74.922
34;Se;Sélénium;78.971
35;Br;Brome;79.904
36;Kr;Krypton;83.798
37;Rb;Rubidium;85.468
38;Sr;Strontium;87.62
39;Y;Yttrium;88.906
40;Zr;Zirconium;91.224
41;Nb;Niobium;92.906
42;Mo;Molybdène;95.95
43;Tc;Technétium;98.0
44;Ru;Ruthénium;101.07
45;Rh;Rhodium;102.91
46;Pd;Palladium;106.42
47;Ag;Argent;107.87
48;Cd;Cadmium;112.41
49;In;Indium;114.82
50;Sn;Étain;118.71
51;Sb;Antimoine;121.76
52;Te;Tellure;127.6
53;I;Iode;126.9
54;Xe;Xénon;131.29
55;Cs;Césium;132.91
56;Ba;Baryum;137.33
57;La;Lanthane;138.91
58;Ce;Cérium;140.12
59;Pr;Praséodyme;140.91
60;Nd;Néodyme;144.24
61;Pm;Prométhium;145.0
62;Sm;Samarium;150.36
63;Eu;Europium;151.96
64;Gd;Gadolinium;157.25
65;Tb;Terbium;158.93
66;Dy;Dysprosium;162.5
67;Ho;Holmium;164.93
68;Er;Erbium;167.26
69;Tm;Thulium;168.93
70;Yb;Ytterbium;173.05
71;Lu;Lutécium;174.97
72;Hf;Hafnium;178.49
73;Ta;Tantale;180.95
74;W;Tungstène;183.84
75;Re;Rhénium;186.21
76;Os;Osmium;190.23
77;Ir;Iridium;192.22
78;Pt;Platine;195.08
79;Au;Or;196.97
80;Hg;Mercure;200.59
81;Tl;Thallium;204.38
82;Pb;Plomb;207.2
83;Bi;Bismuth;208.98
84;Po;Polonium;209.0
85;At;Astate;210.0
86;Rn;Radon;222.0
87;Fr;Francium;223.0
88;Ra;Radium;226.0
89;Ac;Actinium;227.0
90;Th;Thorium;232.04
91;Pa;Protactinium;231.04
92;U;Uranium;238.03
93;Np;Neptunium;237.0
94;Pu;Plutonium;244.0
95;Am;Américium;243.0
96;Cm;Curium;247.0
97;Bk;Berkélium;247.0
98;Cf;Californium;251.0
99;Es;Einsteinium;252.0
100;Fm;Fermium;257.0
101;Md;Mendélévium;258.0
102;No;Nobélium;259.0
103;Lr;Lawrencium;266.0
104;Rf;Rutherfordium;267.0
105;Db;Dubnium;268.0
106;Sg;Seaborgium;269.0
107;Bh;Bohrium;270.0
108;Hs;Hassium;277.0
109;Mt;Meitnérium;278.0
110;Ds;Darmstadtium;281.0
111;Rg;Roentgenium;282.0
112;Cn;Copernicium;285.0
113;Nh;Nihonium;286.0
114;Fl;Flérovium;289.0
115;Mc;Moscovium;290.0
116;Lv;Livermorium;293.0
117;Ts;Tennessine;294.0
118;Og;Oganesson;294.0";

        // ── Utilisations courantes (teen-friendly, pratiques) ─────────────────────
        private static readonly Dictionary<string, string> CommonUses = new(StringComparer.OrdinalIgnoreCase)
        {
            ["H"] = "Carburant de fusée, piles à combustible, fabrication des engrais agricoles",
            ["He"] = "Ballons de fête, appareils IRM dans les hôpitaux, protection des soudures",
            ["Li"] = "Batteries de smartphones et voitures électriques, médicaments psychiatriques",
            ["Be"] = "Composants d'avions et de satellites (léger et très solide), alliages spéciaux",
            ["B"] = "Verre borosilicate (Pyrex), borax (antiseptique), écrans LCD",
            ["C"] = "Plastiques, combustibles fossiles, graphite dans les crayons, diamants, médicaments",
            ["N"] = "Engrais agricoles (nourrit la moitié de l'humanité!), sachets de chips, azote liquide",
            ["O"] = "Respirateurs d'hôpitaux, soudure industrielle, combustion des moteurs",
            ["F"] = "Dentifrice (fluor protège les dents), revêtement Teflon des poêles, réfrigérants",
            ["Ne"] = "Enseignes lumineuses colorées, lasers, décoration",
            ["Na"] = "Sel de cuisine (NaCl), éclairage des tunnels (lampes orange), verre, savon",
            ["Mg"] = "Pièces légères de voiture et vélo, médicaments anti-acides, feux d'artifice blancs",
            ["Al"] = "Canettes de boisson, papier aluminium, avions, cadres de vélo, emballages",
            ["Si"] = "Microprocesseurs de ton téléphone, panneaux solaires, verre, silicone",
            ["P"] = "Engrais, allumettes, acide phosphorique dans les sodas, ADN de toutes les cellules",
            ["S"] = "Pneus de voiture (caoutchouc vulcanisé), médicaments, acide sulfurique industriel",
            ["Cl"] = "Désinfection de l'eau potable, PVC (tuyaux), produits ménagers, piscines",
            ["Ar"] = "Ampoules électriques, soudure TIG, protection des tableaux anciens dans les musées",
            ["K"] = "Engrais agricoles, aliments (bananes et pommes de terre en sont riches!), médecine",
            ["Ca"] = "Béton et ciment, lait et produits laitiers, os et dents, mortier, antiacides",
            ["Sc"] = "Alliages pour cadres de vélo et raquettes de sport haute performance",
            ["Ti"] = "Implants dentaires et orthopédiques, fuselage d'avion militaire, montures de lunettes",
            ["V"] = "Acier allié pour outils (clés à molette), catalyseur chimique industriel",
            ["Cr"] = "Acier inoxydable (couteaux, éviers), chromage décoratif, peintures et teintures",
            ["Mn"] = "Acier pour rails et casques, batteries alcalines, colorant violet du verre",
            ["Fe"] = "Construction (béton armé, charpentes), moteurs, transport du sang (hémoglobine)",
            ["Co"] = "Batteries rechargeables de smartphones et VE, alliages pour turbines, aimants",
            ["Ni"] = "Acier inoxydable, pièces de monnaie, batteries Li-Ni, revêtements anti-corrosion",
            ["Cu"] = "Fils électriques, tuyaux de plomberie, pièces de monnaie, circuits imprimés",
            ["Zn"] = "Galvanisation de l'acier contre la rouille, alliage laiton, pommades cicatrisantes",
            ["Ga"] = "LED bleues et vertes (dans ton téléphone!), semi-conducteurs, thermomètres médicaux",
            ["Ge"] = "Fibres optiques, panneaux solaires haute performance, semi-conducteurs",
            ["As"] = "Semi-conducteurs, bois traité contre les insectes, anciens pesticides",
            ["Se"] = "Cellules photoélectriques, photocopieurs, verres colorés, alimentation animale",
            ["Br"] = "Retardateurs de feu dans les plastiques, médicaments sédatifs, traitement des piscines",
            ["Kr"] = "Lasers médicaux (chirurgie des yeux), éclairage studio photo, lampes flash",
            ["Rb"] = "Horloges atomiques ultra-précises pour GPS et 5G, spectroscopie laser",
            ["Sr"] = "Feux d'artifice rouges (strontium = couleur rouge!), matériaux dentaires",
            ["Y"] = "Phosphore des écrans LED blancs, alliages haute température, yttres médicaux",
            ["Zr"] = "Gaines de combustible nucléaire, couronnes dentaires en céramique, couteaux céramique",
            ["Nb"] = "Acier haute résistance pour pipelines pétroliers et pont, supraconducteurs IRM",
            ["Mo"] = "Acier allié haute résistance (cadres de vélo pro), lubrifiants, catalyseurs",
            ["Tc"] = "Médecine nucléaire : scintigraphie pour détecter les cancers et maladies cardiaques",
            ["Ru"] = "Catalyseurs automobiles, pointes de stylos billes durables, stockage de données",
            ["Rh"] = "Pots catalytiques des voitures (réduit la pollution), électronique de précision",
            ["Pd"] = "Pots catalytiques des voitures, bijouterie (or blanc), purification de l'hydrogène",
            ["Ag"] = "Bijoux, couverts, miroirs, photographie argentique, pansements antibactériens",
            ["Cd"] = "Anciennes batteries Ni-Cd rechargeables, cellules solaires CdTe, pigments jaunes",
            ["In"] = "Écrans tactiles (couche ITO), soudures basse température, semi-conducteurs",
            ["Sn"] = "Boîtes de conserve alimentaires, soudures électroniques, bronze (alliage Cu-Sn)",
            ["Sb"] = "Retardateurs de feu dans les plastiques électroniques, batteries, semi-conducteurs",
            ["Te"] = "Cellules solaires au tellurure de cadmium, alliages, thermoélectriques",
            ["I"] = "Sel iodé (prévient le goitre), désinfectant (teinture d'iode), traitement thyroïde",
            ["Xe"] = "Phares xénon des voitures, anesthésique médical, propulseur de satellites",
            ["Cs"] = "Horloges atomiques (définissent la seconde depuis 1967!), photoélectricité",
            ["Ba"] = "Sulfate de baryum pour la radio de l'intestin en médecine, verre optique, peintures",
            ["La"] = "Lentilles optiques haute qualité, batteries NiMH des voitures hybrides",
            ["Ce"] = "Pots catalytiques, pierres à briquet (ferrocérium), polissage du verre",
            ["Pr"] = "Aimants permanents puissants pour moteurs VE, verres de soudeur (protection UV)",
            ["Nd"] = "Aimants néodyme : dans tes haut-parleurs, disques durs, moteurs de voiture électrique",
            ["Pm"] = "Usage médical très spécialisé (mesures d'épaisseur industrielle), piles atomiques",
            ["Sm"] = "Aimants samarium-cobalt (moteurs industriels, robots), traitement ciblé du cancer",
            ["Eu"] = "Phosphore des écrans TV et smartphones, sécurité des billets d'euro (fluorescence UV)",
            ["Gd"] = "Agent de contraste pour l'IRM en médecine, contrôle dans les réacteurs nucléaires",
            ["Tb"] = "Phosphore vert des écrans, matériaux magnétostrictifs pour les sonars marins",
            ["Dy"] = "Aimants des moteurs de voitures électriques, lasers, disques durs informatiques",
            ["Ho"] = "Aimants les plus puissants des laboratoires de physique, lasers médicaux",
            ["Er"] = "Amplificateurs dans les fibres optiques, lasers chirurgicaux en dermatologie",
            ["Tm"] = "Appareils rayons X portables (dentisterie), lasers médicaux, recherche",
            ["Yb"] = "Lasers ultraprécis, horloges atomiques de nouvelle génération, dopage des fibres optiques",
            ["Lu"] = "Scanner PET-scan en médecine nucléaire, catalyseurs de raffinage pétrolier",
            ["Hf"] = "Transistors des microprocesseurs modernes, barres de contrôle des réacteurs nucléaires",
            ["Ta"] = "Condensateurs électroniques (dans tous les smartphones!), implants chirurgicaux",
            ["W"] = "Filaments des ampoules à incandescence, outils de coupe industriels, munitions",
            ["Re"] = "Alliages pour turbines de réacteur d'avion, filaments de fours industriels",
            ["Os"] = "Pointes de stylos billes de luxe, instruments chirurgicaux, alliages très durs",
            ["Ir"] = "Bougies d'allumage haute performance, pointes de stylos, alliages résistants",
            ["Pt"] = "Pots catalytiques des voitures, bijoux, électrodes médicales, piles à hydrogène",
            ["Au"] = "Bijoux, connecteurs électriques (smartphones et ordinateurs), traitement anti-cancer",
            ["Hg"] = "Thermomètres et baromètres, ampoules fluorescentes, amalgame dentaire (ancien)",
            ["Tl"] = "Détecteurs gamma en médecine nucléaire, verres optiques infrarouges",
            ["Pb"] = "Batteries de démarrage des voitures, blindage contre les rayonnements X en médecine",
            ["Bi"] = "Médicaments contre les maux de ventre (Pepto-Bismol!), cosmétiques nacrés, alliages",
            ["Po"] = "Source de chaleur dans les sondes spatiales, usage médical très spécialisé",
            ["At"] = "Recherche médicale expérimentale pour traitement ciblé des cancers",
            ["Rn"] = "Utilisé en radiothérapie dans certains hôpitaux (mais dangereux dans les maisons!)",
            ["Fr"] = "Recherche fondamentale uniquement (trop instable et rare pour autre chose)",
            ["Ra"] = "Médecine nucléaire (historique), autrefois dans les cadrans lumineux des montres",
            ["Ac"] = "Médecine nucléaire : traitement du cancer de la prostate (Ac-225), recherche",
            ["Th"] = "Combustible nucléaire de 4e génération, anciennement dans les manteaux de camping",
            ["Pa"] = "Recherche fondamentale uniquement",
            ["U"] = "Combustible des centrales nucléaires, armes nucléaires, datation géologique",
            ["Np"] = "Production de Pu-238 pour les piles des sondes spatiales, détecteurs de neutrons",
            ["Pu"] = "Combustible nucléaire, piles thermoélectriques des sondes spatiales (Voyager!)",
            ["Am"] = "Détecteurs de fumée domestiques (en très petite quantité!), médecine nucléaire",
            ["Cm"] = "Sources de chaleur pour missions spatiales, recherche en neutronographie",
            ["Bk"] = "Recherche fondamentale : synthèse d'éléments encore plus lourds",
            ["Cf"] = "Démarrage des réacteurs nucléaires, détecteurs d'or et d'argent dans les mines",
            ["Es"] = "Recherche fondamentale uniquement",
            ["Fm"] = "Recherche fondamentale uniquement",
            ["Md"] = "Recherche fondamentale uniquement",
            ["No"] = "Recherche fondamentale uniquement",
            ["Lr"] = "Recherche fondamentale uniquement",
        };

        // ── Où le trouver dans la vie quotidienne ─────────────────────────────────
        private static readonly Dictionary<string, string> WhereToFinds = new(StringComparer.OrdinalIgnoreCase)
        {
            ["H"] = "Dans l'eau (H₂O), dans le Soleil et les étoiles, dans les combustibles fossiles",
            ["He"] = "Extrait du gaz naturel sous terre, dans les ballons et les IRM des hôpitaux",
            ["Li"] = "Mines du triangle du lithium (Chili, Argentine, Bolivie), batteries de ton téléphone",
            ["Be"] = "Minerai de béryl (dont l'émeraude et l'aigue-marine sont des variétés!), alliages aéro",
            ["B"] = "Sols désertiques (Turquie, USA), détergents, verre Pyrex dans ta cuisine",
            ["C"] = "Partout! Charbon, pétrole, diamant, graphite (crayons), tous les êtres vivants",
            ["N"] = "Dans l'air que tu respires (78%!), engrais agricoles, protéines alimentaires",
            ["O"] = "Dans l'air (21%), dans l'eau, dans presque tous les minéraux et roches",
            ["F"] = "Dentifrice et eau du robinet (eau fluorée), minerai de fluorite, Teflon des poêles",
            ["Ne"] = "Dans l'air en quantité infime (0,002%), enseignes lumineuses",
            ["Na"] = "Sel de table et sel marin, eau de mer, nombreux aliments (pain, charcuterie)",
            ["Mg"] = "Eau de mer, magnésite, légumes verts (il est dans la chlorophylle!), médicaments",
            ["Al"] = "Bauxite (minerai commun), canettes et papier alu, avions, cadres de vélo autour de toi",
            ["Si"] = "Sable de plage (SiO₂!), quartz, granit, microprocesseur de ton téléphone",
            ["P"] = "Engrais agricoles, os et dents, ADN de toutes les cellules vivantes, sodas (E338)",
            ["S"] = "Volcans, minerais de pyrite, pneus, médicaments, œufs (odeur de soufre!)",
            ["Cl"] = "Sel marin (NaCl), eau de piscine, PVC des tuyaux, nombreux médicaments",
            ["Ar"] = "Dans l'air (0,93%), ampoules électriques, bouteilles de vin (conservation)",
            ["K"] = "Bananes, pommes de terre, légumes, engrais agricoles, eaux minérales",
            ["Ca"] = "Lait et fromage, béton et calcaire autour de toi, os, eau calcaire du robinet",
            ["Sc"] = "Minerais rares en Scandinavie et Chine, vélos et raquettes haut de gamme",
            ["Ti"] = "Sable minéral (ilménite, rutile), implants dentaires, avions militaires, lunettes",
            ["V"] = "Minerai de vanadinite, outils en acier allié, pétrole brut (en traces)",
            ["Cr"] = "Minerai de chromite, acier inoxydable de ta cuisine, pièces chromées de voiture",
            ["Mn"] = "Minerai de pyrolusite, nœuds de manganèse au fond des océans, batteries alcalines",
            ["Fe"] = "Partout! Minerai de fer (hématite), charpentes, voitures, sang humain (hémoglobine)",
            ["Co"] = "Minerai de cobaltite (Congo principalement), batteries de ton téléphone",
            ["Ni"] = "Minerai de pentlandite, pièces de 1€ et 2€, acier inoxydable, météorites!",
            ["Cu"] = "Mines de cuivre (Chili, 1er mondial), fils électriques, tuyaux de ta maison",
            ["Zn"] = "Minerai de sphalérite, acier galvanisé des barrières, pommades de pharmacie",
            ["Ga"] = "Sous-produit de l'aluminium et du zinc, LED de ton téléphone et tes écrans",
            ["Ge"] = "Sous-produit du zinc et du charbon, fibres optiques, semi-conducteurs",
            ["As"] = "Minerai d'arsénopyrite, bois traité (certaines terrasses), semi-conducteurs",
            ["Se"] = "Sous-produit de la fusion du cuivre, photocopieurs, certains aliments",
            ["Br"] = "Eau de mer et lacs salés, plastiques électroniques (retardateurs de feu)",
            ["Kr"] = "Dans l'air en quantité infime (1 ppm), lasers médicaux, studios photo",
            ["Rb"] = "Minerai de feldspath, sous-produit du lithium, horloges atomiques des GPS",
            ["Sr"] = "Minerai de célestine, feux d'artifice rouges (couleur rouge = strontium!), eau de mer",
            ["Y"] = "Minerai de gadolinite, terres rares (Chine), phosphore des écrans",
            ["Zr"] = "Sable de zircon (de nombreuses plages!), couronnes dentaires, réacteurs nucléaires",
            ["Nb"] = "Minerai de coltan (Afrique centrale), acier haute résistance des pipelines",
            ["Mo"] = "Minerai de molybdénite, acier allié des outils et cadres de vélo pro",
            ["Tc"] = "N'existe pas à l'état naturel! Uniquement créé dans les réacteurs nucléaires",
            ["Ru"] = "Sous-produit du nickel et du platine (Russie, Afrique du Sud), très rare",
            ["Rh"] = "Avec le platine et le palladium (Afrique du Sud), pot catalytique de ta voiture",
            ["Pd"] = "Mines d'Afrique du Sud et de Russie, pot catalytique, bijoux en or blanc",
            ["Ag"] = "Mines d'argent (Mexique, Pérou), bijoux, couverts, anciens appareils photo",
            ["Cd"] = "Sous-produit du zinc, anciennes piles rechargeables, cellules solaires",
            ["In"] = "Sous-produit du zinc, écrans tactiles de smartphones et tablettes (couche ITO)",
            ["Sn"] = "Minerai de cassitérite (Chine, Indonésie), boîtes de conserve, soudures électroniques",
            ["Sb"] = "Minerai de stibine, plastiques électroniques ignifugés, batteries",
            ["Te"] = "Sous-produit de la fusion du cuivre, cellules solaires, alliages",
            ["I"] = "Sel iodé dans ta cuisine, algues marines, désinfectant en pharmacie",
            ["Xe"] = "Dans l'air (0,000009%), phares xénon des voitures, hôpitaux (anesthésie)",
            ["Cs"] = "Minerai de pollucite, horloges atomiques des GPS et satellites",
            ["Ba"] = "Minerai de barytine, hôpitaux (radio de l'intestin), eau de mer",
            ["La"] = "Terres rares de Chine, batteries NiMH des voitures hybrides, verres optiques",
            ["Ce"] = "Le plus abondant des terres rares! Pots catalytiques, pierres à briquet",
            ["Pr"] = "Terres rares (Chine, 80% de la production), aimants puissants",
            ["Nd"] = "Terres rares (Chine), aimant dans ton haut-parleur et moteur de voiture électrique",
            ["Pm"] = "N'existe pas à l'état naturel! Créé dans les réacteurs nucléaires",
            ["Sm"] = "Terres rares (Chine, Russie), aimants industriels et équipements militaires",
            ["Eu"] = "Terres rares (très peu), phosphore rouge des écrans, sécurité des billets d'euro",
            ["Gd"] = "Terres rares, hôpitaux (produit de contraste pour IRM)",
            ["Tb"] = "Terres rares, phosphore vert des écrans de ton téléphone",
            ["Dy"] = "Terres rares (Chine principalement), aimants des moteurs de voitures électriques",
            ["Ho"] = "Terres rares, laboratoires de physique fondamentale",
            ["Er"] = "Terres rares, câbles en fibres optiques (amplificateurs), lasers médicaux",
            ["Tm"] = "Terres rares (très rare), appareils médicaux portables",
            ["Yb"] = "Terres rares, horloges atomiques de nouvelle génération, fibres optiques",
            ["Lu"] = "Terres rares (le plus rare des lanthanides!), scanners PET médicaux",
            ["Hf"] = "Minerai de zircon (avec le zirconium), microprocesseurs modernes",
            ["Ta"] = "Minerai de coltan (Congo - conflit géopolitique!), condensateurs de ton téléphone",
            ["W"] = "Minerai de wolframite et scheelite, filaments d'ampoule, outils de coupe",
            ["Re"] = "Sous-produit du molybdène (Chili), turbines de réacteurs d'avion",
            ["Os"] = "Minerai de platine (Afrique du Sud, Russie), très rare dans la nature",
            ["Ir"] = "Météorites! (couche K-Pg = extinction des dinosaures), minerai de platine",
            ["Pt"] = "Mines d'Afrique du Sud (80% de la production mondiale!), pot catalytique de ta voiture",
            ["Au"] = "Mines d'or (Chine, Australie, Russie), bijoux, circuits imprimés de ton téléphone",
            ["Hg"] = "Minerai de cinabre (Espagne, Chine), vieux thermomètres, ampoules fluorescentes",
            ["Tl"] = "Sous-produit du zinc et du plomb, appareils de détection médicale",
            ["Pb"] = "Minerai de galène, batterie de démarrage de voiture, blindage radiologique",
            ["Bi"] = "Sous-produit du plomb, médicaments estomac en pharmacie (Pepto-Bismol!)",
            ["Po"] = "Produit dans les réacteurs nucléaires, traces infimes dans le sol",
            ["At"] = "Créé seulement dans les accélérateurs de particules, très éphémère",
            ["Rn"] = "Dans certaines caves et sous-sols (gaz radioactif naturel, risque cancer!)",
            ["Fr"] = "N'existe pas à l'état naturel en quantité mesurable",
            ["Ra"] = "Minerais d'uranium, musées de physique, laboratoires médicaux (très rare)",
            ["Ac"] = "Sous-produit des mines d'uranium, services d'oncologie des hôpitaux",
            ["Th"] = "Minerai de monazite, sables noirs de plage, anciens manteaux de camping",
            ["Pa"] = "Minerais d'uranium, extrêmement rare et radioactif",
            ["U"] = "Mines d'uranium (Kazakhstan, Canada, Australie), centrales nucléaires",
            ["Np"] = "Créé dans les réacteurs nucléaires à partir de l'uranium",
            ["Pu"] = "Réacteurs nucléaires, très rare dans la nature (traces dans l'uranium)",
            ["Am"] = "Dans le détecteur de fumée de ta maison (en quantité infime et sans danger!)",
            ["Cm"] = "Réacteurs nucléaires, laboratoires de recherche",
            ["Bk"] = "Créé uniquement en accélérateur de particules, demi-vie très courte",
            ["Cf"] = "Réacteurs nucléaires de recherche, laboratoires spécialisés",
            ["Es"] = "Créé uniquement en accélérateur, nommé en hommage à Einstein",
            ["Fm"] = "Créé dans les essais nucléaires (découvert en 1952!), accélérateur de particules",
            ["Md"] = "Créé uniquement en accélérateur, nommé en hommage à Mendeleïev",
            ["No"] = "Créé uniquement en accélérateur, nommé en hommage à Alfred Nobel",
            ["Lr"] = "Créé uniquement en accélérateur, nommé en hommage à Ernest Lawrence",
        };

        // ── Anecdotes scientifiques ───────────────────────────────────────────────
        private static readonly Dictionary<string, string> FunFacts = new(StringComparer.OrdinalIgnoreCase)
        {
            ["H"] = "L'hydrogène est l'élément le plus abondant de l'univers — 75% de toute la matière visible!",
            ["He"] = "L'hélium est si léger qu'il s'échappe continuellement dans l'espace. La Terre en perd chaque jour!",
            ["Li"] = "Le lithium est si léger qu'il flotte sur l'eau... et réagit vivement avec elle!",
            ["Be"] = "L'émeraude et l'aigue-marine sont des minerais de béryllium colorés par des impuretés de chrome ou de fer.",
            ["B"] = "Le bore donne sa dureté exceptionnelle au verre Pyrex qui résiste aux chocs thermiques.",
            ["C"] = "Le carbone est la base de toute vie sur Terre. Le diamant et le graphite (crayon) sont tous les deux du carbone pur!",
            ["N"] = "L'azote liquide à -196°C est utilisé pour congeler des cellules vivantes et même des embryons!",
            ["O"] = "L'oxygène représente 65% de la masse du corps humain et 21% de l'air.",
            ["F"] = "Le fluor est l'élément le plus électronégatif et le plus réactif de tous les éléments!",
            ["Ne"] = "Les vraies enseignes 'néon' rouges contiennent du néon pur. Les autres couleurs utilisent d'autres gaz.",
            ["Na"] = "Le sodium réagit violemment avec l'eau en produisant de l'hydrogène qui peut s'enflammer!",
            ["Mg"] = "Le magnésium brûle d'une flamme blanche éblouissante et continue de brûler même sous l'eau.",
            ["Al"] = "L'aluminium est le métal le plus abondant dans la croûte terrestre, mais il était plus précieux que l'or au XIXe siècle!",
            ["Si"] = "Le sable est principalement du silicium (SiO₂). Nos puces électroniques sont faites de sable ultra-purifié!",
            ["P"] = "Le phosphore blanc brille dans le noir (phosphorescence) et s'enflamme spontanément à l'air.",
            ["S"] = "Le soufre donne leur odeur caractéristique aux œufs pourris et aux volcans.",
            ["Cl"] = "Le chlore gazeux a été utilisé comme arme chimique lors de la Première Guerre mondiale.",
            ["Ar"] = "L'argon a été découvert en 1894. Son nom vient du grec 'argos' qui signifie 'inactif, paresseux'.",
            ["K"] = "Le symbole K vient du latin 'Kalium'. Une banane contient environ 0,4g de potassium!",
            ["Ca"] = "Nos os et dents sont principalement du phosphate de calcium. Le corps humain contient ~1kg de calcium!",
            ["Ti"] = "Le titane est aussi solide que l'acier mais 45% plus léger. Parfait pour les avions et les implants!",
            ["Cr"] = "Le chrome donne sa couleur verte à l'émeraude et sa couleur rouge au rubis!",
            ["Mn"] = "Il y a d'énormes gisements de manganèse sur le fond des océans sous forme de 'nodules polymétalliques'.",
            ["Fe"] = "Le fer dans notre sang (hémoglobine) est littéralement identique au fer des météorites qui tombent sur Terre!",
            ["Co"] = "Le cobalt donne sa couleur bleue au verre depuis l'Antiquité. Le bleu cobalt est un pigment célèbre.",
            ["Ni"] = "Les pièces de 1€ et 2€ contiennent du nickel et sont donc légèrement magnétiques!",
            ["Cu"] = "Le cuivre est l'un des premiers métaux utilisés par l'humanité, il y a plus de 10 000 ans.",
            ["Zn"] = "Le zinc est essentiel à notre corps : il est impliqué dans plus de 300 réactions enzymatiques!",
            ["Ga"] = "Le gallium fond dans la main (29,8°C)! On peut en acheter pour faire des blagues avec des cuillères qui fondent.",
            ["Ge"] = "Le germanium a été prédit par Mendeleïev en 1871 avant même d'être découvert (en 1886)!",
            ["As"] = "L'arsenic a longtemps été surnommé 'la poudre des héritiers' car il était utilisé pour empoisonner discrètement.",
            ["Br"] = "Le brome est l'un des seuls éléments non métalliques liquides à température ambiante (avec le mercure).",
            ["Kr"] = "Le krypton a inspiré le nom de 'Kryptonite', la faiblesse de Superman dans les comics!",
            ["Xe"] = "Le xénon peut agir comme anesthésique général et comme propulseur ionique dans les satellites. Gaz noble, il peut pourtant former des composés chimiques!",
            ["Ag"] = "L'argent a les meilleures propriétés de conduction électrique et thermique de tous les métaux.",
            ["Sn"] = "L'étain (tin en anglais) donne son nom aux boîtes de conserve (tin cans). Sa dégradation à froid est le 'mal de l'étain'.",
            ["I"] = "L'iode est essentiel à la production des hormones thyroïdiennes. Une carence cause le goitre!",
            ["Cs"] = "L'horloge atomique au césium définit la seconde depuis 1967. Elle ne dérègle pas d'une seconde en 300 millions d'ans!",
            ["Ba"] = "Le baryum en suspension dans l'eau opacifie les rayons X, permettant de visualiser le tube digestif à l'hôpital.",
            ["La"] = "Le lanthane est utilisé dans les batteries des voitures hybrides (Toyota Prius en contient 10-15kg!).",
            ["Ce"] = "Le cérium est l'élément des terres rares le plus abondant dans la croûte terrestre.",
            ["Nd"] = "Les aimants néodyme sont les aimants permanents les plus puissants. Ils sont dangereux : deux aimants de 5cm peuvent se pincer et casser les doigts!",
            ["Eu"] = "L'europium sert à authentifier les billets d'euro : il émet une fluorescence caractéristique sous UV!",
            ["Gd"] = "Le gadolinium est fortement paramagnétique — il répond aux champs magnétiques et sert de contraste dans les IRM.",
            ["W"] = "Le tungstène a le point de fusion le plus élevé de tous les éléments : 3 422°C! Son symbole W vient de Wolfram.",
            ["Os"] = "L'osmium est l'élément le plus dense naturel : il est deux fois plus lourd que le plomb!",
            ["Ir"] = "Un enrichissement mondial en iridium dans les roches il y a 66 millions d'ans prouve l'impact d'un astéroïde qui a tué les dinosaures!",
            ["Pt"] = "Le platine est si inerte qu'il sert à fabriquer des étalons de mesure (le kilogramme étalon était en platine-iridium).",
            ["Au"] = "L'or est si malléable qu'on peut en faire des feuilles si fines (100nm) qu'elles laissent passer la lumière verte!",
            ["Hg"] = "Le mercure est le seul métal liquide à température ambiante. Son symbole Hg vient du latin 'Hydrargyrum' (eau d'argent).",
            ["Pb"] = "Le symbole Pb vient du latin 'plumbum'. Les plombiers et la plomberie doivent leur nom au plomb, autrefois utilisé pour les tuyaux.",
            ["Bi"] = "Le bismuth est le métal le moins toxique des métaux lourds. C'est le principe actif du Pepto-Bismol contre les maux de ventre!",
            ["Po"] = "Le polonium a été nommé par Marie Curie en hommage à sa Pologne natale, alors sous domination russe.",
            ["Ra"] = "Le radium découvert par Marie Curie brille faiblement dans le noir à cause de sa radioactivité. Elle en est décédée.",
            ["U"] = "L'uranium naturel est légèrement radioactif. Il a été découvert en 1789, 100 ans avant que la radioactivité soit comprise!",
            ["Pu"] = "La sonde Voyager 1, lancée en 1977 et aujourd'hui hors du système solaire, est alimentée par du plutonium!",
            ["Am"] = "L'américium dans les détecteurs de fumée ionise l'air. Une particule alpha déclenche l'alarme en cas de fumée.",
        };

        public static readonly IReadOnlyList<Element> AllElements = ParseElements();

        private static System.Collections.ObjectModel.ReadOnlyCollection<Element> ParseElements()
        {
            List<Element> list = new List<Element>(120);
            foreach (string line in CsvData.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed))
                {
                    continue;
                }

                string[] parts = trimmed.Split(';');
                if (parts.Length < 4)
                {
                    continue;
                }

                if (!int.TryParse(parts[0], out int atomicNumber))
                {
                    continue;
                }

                if (!decimal.TryParse(parts[3], System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out decimal mass))
                {
                    continue;
                }

                string symbol = parts[1];
                _ = CommonUses.TryGetValue(symbol, out string? use);
                _ = WhereToFinds.TryGetValue(symbol, out string? where);
                _ = FunFacts.TryGetValue(symbol, out string? fact);

                list.Add(new Element(atomicNumber, symbol, parts[2], mass,
                    use ?? "Usage industriel et recherche scientifique",
                    where ?? "Laboratoires de recherche et industrie spécialisée",
                    fact));
            }
            return list.AsReadOnly();
        }
    }
}
