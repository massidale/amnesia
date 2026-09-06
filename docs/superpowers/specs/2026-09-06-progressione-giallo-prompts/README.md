# Prompt proposti — revisione della progressione

Solo bozza: il gioco non carica questi file. La revisione elimina le varianti per domande e fotografie degli abitanti, sostituendole con conoscenze permanenti e la tabella di reazioni già prevista dal codice.

- 14 NPC, 20 schede complete. Scheda unica per Rosa, Lidia, Marisa, Beppe, Teresa, Piero, Gino, Don Carlo ed Elena.
- Posizioni soltanto per Anna (2), Laura (2), Matteo (3), Nino (2), Wanda (2).
- `reazioni.json` usa la struttura reale `Reazioni → NPC → posizione/sempre → oggetto → testo`. È il testo da aggiungere al prompt, non un nuovo script o una nuova conoscenza delle persone.
- `rules.md`, `mondo.md`, `conoscenze.json`, `conoscenze/san_rocco.md` e schede sono la parte destinata al caricatore dopo revisione. Gli altri file sono documentazione/cataloghi editoriali e non vanno copiati dentro `content/prompts`.

Le domande selezionano il contenuto della risposta, senza selezionare una nuova scheda. Lidia e Marisa conoscono Anna senza foto; la foto aggiunge il ricordo delle parole condivise con Pietro. Piero può raccontare le ricerche subito. Matteo dopo l'ammissione sa già spiegare il perché. Il don guida dal racconto e dagli oggetti, senza scale o informazioni automatiche sugli altri incontri.

Nino trova la giacca soltanto dopo che Matteo ha pronunciato Elena viva. Nessun requisito di rito, rifiuto, visita al don o ricerca già avviata sul coma.

Il manifest e i cataloghi sono documenti editoriali: non aggiungere nuovi id o meccaniche per applicarli. Il piano ora ammette soltanto prompt, reazioni nel formato attuale e scalini. Le precedenti proposte di azioni esplicite, battute garantite, memoria aggiuntiva e controlli di possesso sono ritirate.

Matteo M2 gestisce l’accusa nel prompt. Con gli scalini attuali si può usare `nino_giacca` come riscontro della consegna, ma non verificare direttamente il possesso. Foto iniziale, formula nel taccuino, passaparola di Nino e consegne su richiesta presentano incompatibilità documentate nella specifica.

I casi di accettazione includono desiderata narrativi che non sono tutti realizzabili in questo perimetro: non modificarne il motore per farli passare. La foto a otto adulti resta inoltre proposta editoriale da confermare.
