# Verifica della bozza — frase contestuale e guida

Controlli statici eseguiti il 6 settembre 2026:

- JSON leggibili; 14 NPC e 20 schede esistenti, con collegamenti dell’indice validi.
- Anna ha due schede, versione pubblica e confessione del rito. Nessuna posizione per la frase e nessun prerequisito frase/foto per il rito.
- 67 reazioni con NPC, posizioni e oggetti validi. Il ricordo del magazzino è nella reazione alla frase della versione pubblica; le istruzioni di continuità sono in scheda e piano.
- 59 dichiarazioni editoriali; annotazioni del ricordo e del luogo collegate al contesto/reazione, senza grants di una posizione della frase.
- Tutte le 20 schede contengono indicazioni specifiche per dare informazioni e guidare l’indagine; i contenuti riservati restano esclusi dalle schede iniziali pertinenti.
- 32 casi di accettazione referenziano schede esistenti, inclusi frase senza cambio di stato, seguito oltre 12 messaggi, visite precoci e ritorni senza giri ripetuti.

I casi sono ancora da eseguire dopo l’implementazione. Nessun file runtime modificato, nessuna sessione LLM o prova del gioco effettuata in questa revisione. I controlli statici non dimostrano che il modello segua sempre i rimandi: la verifica della conversazione resta distinta dal determinismo degli effetti nel motore.


Revisione Matteo: verificati staticamente condizione M2 per possesso, prompt prima/dopo accusa, reazioni senza confessione automatica e rimozione dell’azione d’accusa proposta. Aggiunto caso di possesso senza presentazione né riconoscimento. Nessun runtime modificato o dialogo LLM eseguito.


Vincolo successivo: ritirate dal piano le estensioni al motore. Controllati nel codice i predicati esistenti, `nino_giacca` con `counts_alone`, consegna automatica e hardcoding di foto/frase/passaparola. Le verifiche precedenti erano statiche della bozza, non prova che tutti i desiderata siano implementabili con soli prompt e scalini.
