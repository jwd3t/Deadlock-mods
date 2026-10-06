# 💀 Sekiro Death & Victory Mod for Deadlock

Pantallas de muerte y victoria al estilo de **Sekiro: Shadows Die Twice**.

---

## ⚔️ Qué hace

1. **Tu muerte**: el kanji rojo **"死"** con **DEATH** debajo, en el centro de la pantalla mientras estás muerto (no durante la repetición de tu muerte), con el sonido de muerte de Sekiro. Suena **una sola vez y solo cuando mueres tú**, en cualquier modo, incluido el Sandbox.
2. **Muerte falsa con el Rejuvenator**: si mueres teniendo el renacer del Rejuvenator, el **"死" sale en gris**, como en Sekiro cuando todavía puedes resucitar.
3. **Muerte falsa de Victor**: durante la canalización de **Shocking Reanimation**, el "死" gris aparece sobre la barra de reanimación.
4. **Victoria**: al ganar, la pantalla de fin de partida muestra **"忍殺" (SHINOBI EXECUTION)** en lugar del logo del equipo, con la fanfarria de victoria de Sekiro.

## 🔧 Correcciones respecto a la versión anterior

* El sonido de muerte de Sekiro sonaba también **cuando moría un aliado o un enemigo** (estaba en los avisos `UI.PlayerDeath.Team/Opponent`). Ahora tiene su propio evento y solo suena en tu muerte.
* El mismo clip se encimaba hasta tres veces al morir (dos eventos de sonido + el CSS). Ahora suena una vez.
* Al resucitar (Victor o Rejuvenator) volvía a sonar el sonido de muerte. Ahora suenan los sonidos originales del juego.
* Los archivos de sonido del mod eran copias viejas del juego y deshacían un cambio de Valve (`Stinger.RevealVote`). Ahora el mod se construye siempre desde los archivos actuales.
* El "忍殺" de victoria estaba descrito pero no existía. Ahora sí.

---

## 🛠️ Construir

```bash
python build.py
```

Extrae del juego instalado las hojas de estilo y eventos de sonido actuales y les aplica solo los cambios de `src/` (CSS en `src/*.css`, eventos de sonido en `src/*.json`). **Hay que volver a ejecutarlo después de cada actualización de Deadlock**, porque esos archivos son únicos para todo el juego. Genera `pak01_dir.vpk` y `Sekiro_Death_Victory_Mod.zip`. Requiere el SDK de .NET (usa `tools/vpcf_tool`).

## 🚀 Instalación

* **Deadlock Mod Manager (recomendado):** arrastra [`Sekiro_Death_Victory_Mod.zip`](Sekiro_Death_Victory_Mod.zip).
* **Manual:** copia [`pak01_dir.vpk`](pak01_dir.vpk) a `<Steam>/steamapps/common/Deadlock/game/citadel/addons/` con el siguiente número libre (por ejemplo `pak37_dir.vpk`).
