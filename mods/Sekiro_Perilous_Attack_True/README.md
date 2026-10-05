# Sekiro Perilous Attack ("危") - Versión True (Fiel a Sekiro)

Mod de aviso de peligro defensivo inspirado en **Sekiro: Shadows Die Twice** para **Deadlock**.

---

## ⚔️ ¿Cómo Funciona la Versión "True"?

La **Versión True** replica la función de alerta defensiva de Sekiro:

1. **Aviso Exclusivo de Peligro Defensivo**:
   * El kanji carmesí `"危"` aparece sobre el héroe cuando un enemigo cercano está cargando un *Heavy Melee*.
   * Funciona como advertencia táctica para que puedas preparar tu desvío con Parry (`F`) o esquivar a tiempo.
2. **Radio de Detección Calibrado a 13 Metros (512 Unidades)**:
   * Controlado mediante `C_OP_DistanceToTransform` vinculado al atacante (`CP0`).
   * Si un enemigo carga melee a más de 13 metros (512 unidades), tanto la opacidad (Alpha) como el radio del kanji se escalan a `0.0`, asegurando que no recibas falsas alarmas de combates lejanos.
3. **Inmunidad Propia (< 95 Unidades)**:
   * Cuando **tú** cargas un heavy melee, la distancia relativa a tu entidad es de 75 unidades en Z. El filtro `C_OP_DistanceCull` con `m_bCullInside: true` (< 95 unidades) destruye el kanji al instante.
   * **No verás el kanji sobre ti cuando seas tú quien da el golpe.**
4. **Altura y Tamaño Calibrados**:
   * Altura fijada en **+75.0 unidades** en Z (repose perfecto sobre la cabeza sin flotar en el aire).
   * Radio reducido a la mitad (**19.0 unidades**) para un telegraph estético, limpio y auténtico a Sekiro.
   * Orientación *Billboard Camera-Facing* permanente.
5. **Audio de Ataque Peligroso Remasterizado**:
   * Incluye el sonido original de peligro de Sekiro a 48,000 Hz estéreo.

---

## 🚀 Instalación

Tienes los archivos listos en esta carpeta:
* 📦 Archivo VPK: [`pak01_dir.vpk`](pak01_dir.vpk)
* 🗜️ Archivo ZIP para Deadlock Mod Manager: [`Sekiro_Perilous_Attack_True.zip`](Sekiro_Perilous_Attack_True.zip)

### Método 1: Deadlock Mod Manager (Recomendado)
Arrastra el archivo [`Sekiro_Perilous_Attack_True.zip`](Sekiro_Perilous_Attack_True.zip) dentro de Deadlock Mod Manager.

### Método 2: Instalación Manual
Copia [`pak01_dir.vpk`](pak01_dir.vpk) y pégalo dentro de tu carpeta de addons de Deadlock (`game/citadel/addons/`).
