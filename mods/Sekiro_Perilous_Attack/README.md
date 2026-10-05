# 🈲 Sekiro Perilous Attack ("危") Mod for Deadlock

Mod para **Deadlock** que transforma la carga del golpe cuerpo a cuerpo pesado (Heavy Melee / `Q`) en el auténtico **Aviso de Ataque Peligroso de Sekiro: Shadows Die Twice**:
- 🈲 **Kanji de Peligro ("危") Fiel al Original**:
  - **Técnica Overhead de Trophy Collector**: Utiliza un desplazamiento vertical (`+110` en Z con `C_OP_PositionLock`) idéntico al trofeo del objeto *Trophy Collector*, garantizando que flote siempre perfectamente **por encima de la cabeza** de cualquier héroe, ya sea de baja estatura (Pocket, Vindicta) o colosos de gran altura (Abrams, Bebop, Seven).
  - **Tamaño Calibrado (Mitad del Tamaño)**: Reducido a la mitad (radio `21.0` en lugar de `42.0`), logrando una advertencia nítida, elegante y fiel al juego original sin obstruir la vista del jugador.
  - **Animación Dinámica**: Aparece con la clásica animación rápida de "pop" (expansión suave de 0.6x a 1.0x con desvanecimiento alfa al finalizar).
- 🔊 **Audio Oficial de Sekiro**: El sonido característico de aviso con ganancia amplificada (+8 dB) y duración completa sin cortes.
- 🧹 **Sin Ruido Visual**: Eliminadas todas las partículas genéricas de chispas en la mano, dejando únicamente el Kanji limpio y centrado.

---

## 📦 Contenido de la Carpeta

- **`pak01_dir.vpk`**: El mod compilado y listo para Deadlock.
- **`Sekiro_Perilous_Attack.zip`**: Archivo para distribuir o cargar en **Deadlock Mod Manager**.
- **`build_mod.bat`**: Reconstruye el mod con 1 doble clic.
- **`build_full_mod.py`**: Empaquetador VPK autónomo en Python.
- **`extracted/`**:
  - `materials/particle/sekiro_kanji.vtex_c` (Textura DXT5 del Kanji "危").
  - `particles/abilities/melee/melee_heavy_activate_charge.vpcf_c` (Sistema de partículas principal).
  - `particles/abilities/melee/melee_heavy_activate_charge_burst.vpcf_c` (Emisor del Kanji con técnica overhead de Trophy Collector).
  - `sounds/player/melee/shared/charged_melee_full.vsnd_c` (Sonido oficial de aviso de Sekiro).

---

## 🚀 Instalación

1. **Deadlock Mod Manager**: Arrastra `Sekiro_Perilous_Attack.zip` a DMM.
2. **Manual**: Copia `pak01_dir.vpk` a tu carpeta de addons de Deadlock (`game/citadel/addons/`).
