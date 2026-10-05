# 💀 Sekiro Death, Victory & Resurrection Mod for Deadlock

Mod atmosférico que transforma la experiencia de muerte y victoria en Deadlock al estilo de **Sekiro: Shadows Die Twice**, implementando pantallas fúnebres con el kanji **"死"**, celebración de victoria con **"忍殺"**, y una mecánica de **Muerte Falsa vs. Muerte Verdadera** para **Victor (`hero_frank`)** y el **Rejuvenator del Mid Boss**.

---

## ⚔️ Características

1. **Pantalla de Muerte Verdadera (True Death)**:
   * Al morir definitivamente, proyecta en el centro de la pantalla el kanji caligráfico carmesí **"死"** (*Muerte*).
   * Reproduce el lamento tradicional de cuerdas japonesas y gong budista de Sekiro (4.728s a 48,000 Hz estéreo) mediante el evento `Stinger.Death`.
2. **Pantalla de Victoria ("忍殺" - Shinobi Execution)**:
   * Al ganar la partida y destruir el Patrón enemigo, reproduce la fanfarria de victoria y el gong triunfal de Sekiro (6.072s a 48,000 Hz estéreo) mediante `Music.Match.Win`.
3. **Muerte Falsa para Victor (`hero_frank`) (100% Panorama HUD 2D)**:
   * **Muerte Falsa**: Cuando Victor recibe daño letal teniendo activa su habilidad máxima de reanimación (`Shocking Reanimation`), se proyecta de forma fluida el kanji sombrío tenue **"死"** en gris con sombra (*Muerte Falsa de Sekiro*) mediante una transición progresiva de opacidad (fade-in) pegada al HUD 2D mientras canaliza el renacer, seguido de la explosión eléctrica.
   * **Muerte Verdadera**: Si la habilidad está en enfriamiento o expira la reanimación temporal, Victor sufre la pantalla de Muerte Verdadera con el kanji carmesí brillante **"死"** con glow y el audio fúnebre completo.
4. **Muerte Falsa con el Rejuvenator (Mid Boss)**:
   * Al caer derrotado portando el Rejuvenator, se activa el audio fúnebre y el estado de resurrección de Sekiro.
5. **Cobertura Total de Audio**:
   * Reemplazo de todos los canales de muerte de equipo, oponente, música y habilidad para garantizar que suene en cualquier situación (partida pública, privada o sala Sandbox contra Target Dummies).

---

## 📦 Archivos Disponibles

* 🗜️ **Para Deadlock Mod Manager (Recomendado)**: [`Sekiro_Death_Victory_Mod.zip`](Sekiro_Death_Victory_Mod.zip)
* 📦 **Para Instalación Manual**: [`pak01_dir.vpk`](pak01_dir.vpk)

---

## 🚀 Instalación

### Opción A: Deadlock Mod Manager (DMM)
Arrastra el archivo [`Sekiro_Death_Victory_Mod.zip`](Sekiro_Death_Victory_Mod.zip) dentro de Deadlock Mod Manager y actívalo.

### Opción B: Instalación Manual
Copia [`pak01_dir.vpk`](pak01_dir.vpk) y renómbralo al siguiente número libre de tu carpeta de addons (por ejemplo `pak37_dir.vpk`):
`<Steam>/steamapps/common/Deadlock/game/citadel/addons/`
