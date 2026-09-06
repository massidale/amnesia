# Verifica della scena giocabile

Teresa seduta sulla panchina della piazza, accanto al bar; anteprima aggiornata
in Unity/Assets/SanRocco1987/Previews/14_teresa.png.

Entrambe le scene salvate contengono Bootstrap, Giocatore, Pannello, Menu e
LuoghiScena1987. Le identita NPC riusano le schede del progetto. Il corpo
del protagonista usa il prefab low poly Giorgio, senza duplicarne l'identita.

La verifica in Play ha prodotto PARTITA_OK:
- 12 NPC a San Rocco e 2 a Chivasso, ognuno riconosciuto e con scheda presente.
- Saluti locali del villaggio registrati; Wanda ed Elena non hanno saluti
  predefiniti nel contenuto e iniziano dal dialogo del giocatore.
- Inventario, mappa e taccuino apribili.
- B-17 rifiutato senza dichiarazione; aperto con chiave e dichiarazione di prova.
- Andata e ritorno effettivi con un giocatore e una camera per scena.
- Stessa sessione, inventario, dichiarazioni e B-17 aperto conservati al ritorno.
- Due ore aggiunte all'orologio per ciascuna tratta.

Le dichiarazioni di prova vivono soltanto nella sessione del test in Play:
non modificano contenuti o stato iniziale della scena salvata.
Controllati visivamente Teresa, inventario e nuova visuale iniziale verso il paese.

Limiti: nessuna chiamata LLM effettuata in questa verifica; le risposte online
richiedono OpenRouter disponibile. La sessione persiste fra scene durante la
partita, non dopo l'uscita da Play o la chiusura del gioco. Routine e pose degli
NPC restano statiche. Non e' stato eseguito un playthrough narrativo completo.
I precedenti video di visita libera non rappresentano l'interfaccia attuale.
