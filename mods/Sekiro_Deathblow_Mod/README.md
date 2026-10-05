# 🩸 Sekiro Deathblow Red Dot Mod for Deadlock

Mod visual y sonoro inspirado en **Sekiro: Shadows Die Twice** que añade el icónico **Punto Rojo de Deathblow (Golpe Mortal)** y el sonido de **Ruptura de Postura / Gong de Ejecución** sobre los enemigos cuando sufren un **Parry exitoso** (`parried_stun`).

---

## ✨ Características

1. **Orbe Carmesí de Deathblow**:
   * Proyecta el punto rojo incandescente de Sekiro centrado sobre el torso del enemigo aturdido por Parry.
   * Textura generada a partir del perfil medido en una captura real de Sekiro (núcleo blanco, anillo naranja, halo rojo puro) con `tools/make_deathblow_texture.py`, compilada en **DXT5 (BC3)** 256x256.
   * Se dibuja con mezcla **aditiva** (`PARTICLE_OUTPUT_BLEND_MODE_ADD`) para que brille como luz, igual que en Sekiro, en vez de verse como una pegatina opaca.
2. **Audio de Impacto y Ruptura de Postura**:
   * Reemplaza el sonido estándar del desvío por el contundente **Gong de Deathblow de Sekiro** (3.024s a 48,000 Hz estéreo).
   * Volumen calibrado a `+5.0 dB` para una presencia acústica contundente sin saturar el audio del juego.
3. **100% Seguro con VAC**:
   * Solo modifica archivos cosméticos del lado del cliente (`.vpcf_c`, `.vtex_c`, `.vsnd_c`, `.vsndevts_c`).

---

## 📦 Archivos Disponibles

* 🗜️ **Para Deadlock Mod Manager (Recomendado)**: [`Sekiro_Deathblow_Mod.zip`](Sekiro_Deathblow_Mod.zip)
* 📦 **Para Instalación Manual**: [`pak01_dir.vpk`](pak01_dir.vpk)

---

## 🚀 Instalación

### Opción A: Deadlock Mod Manager (DMM)
Arrastra el archivo [`Sekiro_Deathblow_Mod.zip`](Sekiro_Deathblow_Mod.zip) dentro de Deadlock Mod Manager y actívalo.

### Opción B: Instalación Manual
Copia [`pak01_dir.vpk`](pak01_dir.vpk) y renómbralo al siguiente número libre de tu carpeta de addons (por ejemplo `pak36_dir.vpk`):
`<Steam>/steamapps/common/Deadlock/game/citadel/addons/`
