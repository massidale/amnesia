# Piano rivisto — soltanto prompt e scalini

Stato: applicazione dei contenuti compatibili effettuata su richiesta dell’utente. Il resoconto effettivo è in `../implementation/2026-09-06-narrativa-applicata.md`; include gli adattamenti necessari per il vocabolario del magazzino e la consegna della lettera con le meccaniche esistenti. Nessuna modifica al motore.

## Perimetro

Modificare schede NPC, regole, conoscenze condivise, testi contestuali in `reazioni.json` e scalini in `positions.json`. Usare i formati, gli id di dichiarazione e le meccaniche già presenti. Nessuna nuova azione UI, condizione di possesso, interpretazione strutturata della domanda, gestione della memoria, battuta automatica, modifica di inizializzazione o salvataggio.

I testi canonici delle dichiarazioni entrano nel prompt/fallback: verificare e segnalare quelli incompatibili. Il catalogo editoriale con nuovi id non va installato come se fosse richiesto da questo piano.

## 1. Riconciliare i contenuti

- Preservare le modifiche locali già presenti nel checkout.
- Confrontare ciascuna scheda proposta con i testi effettivamente ricevuti dal modello: regole, conoscenze, reazioni e dichiarazioni canoniche.
- Correggere la voce, la profondità personale e i rimandi, senza inventare informazioni o nuovi eventi per sostenere il percorso.
- Tenere distinti i requisiti realizzabili e le incompatibilità della sezione 5 della specifica. Non cambiare silenziosamente la storia per aggirarle.

## 2. Riscrivere i prompt e le reazioni

- Scheda unica per i personaggi senza cambi reali; informazioni ordinarie disponibili da subito.
- Foto ai paesani: episodio e frase nella reazione, persone nelle conoscenze generali.
- Anna: frase contestuale in A0; A1 soltanto per il rito. Continuare sui dettagli nel contesto senza pretendere di ripetere le parole.
- Laura: versione pubblica e confessione ai reperti, senza formula.
- Matteo: M0 pubblico, M1 salvataggio, M2 giacca/confronto; M2 rimane evasivo fino all’accusa diretta e conserva l’ammissione nei seguiti.
- Nino: ritrovamento del 1985 disponibile da subito; giacca trovata ora soltanto dopo Elena viva detta da Matteo.
- Don: informazioni e rimandi basati su oggetti e racconti, senza conoscere lo stato globale.
- Prima un fatto utile, poi una persona e cosa può chiarire se serve proseguire; niente giri ripetuti.

## 3. Configurare gli scalini con i soli campi esistenti

- Rimuovere gli scalini basati su normali argomenti o sulla frase.
- Conservare `requires_any_shown` per le confessioni sui reperti.
- Nino N1: `requires_declared: [elena_viva]`, `consegna: [giacca]`.
- Matteo M2: `requires_declared: [nino_giacca]`. È il riscontro narrativo della consegna, non un nuovo controllo di possesso; l’equivalenza dipende dall’emissione della dichiarazione già prevista.
- Anna: lasciare `magazzino_dove` fuori dai grants della scala, così usa il canale per-oggetto/frase già presente in `DeclarationService`.
- Riusare id esistenti. Non aggiungere un campo sconosciuto: il caricatore lo rifiuterebbe.
- Non mettere la lettera in `consegna` di M2: la giacca da sola non è una confessione. Il requisito «dopo richiesta» non è esprimibile con la logica attuale e resta da risolvere sul piano narrativo, senza codice aggiuntivo.

## 4. Verificare e applicare soltanto il contenuto compatibile

- Validare JSON, percorsi, id, grants cumulativi e fallback delle reazioni.
- Usare i test e gli strumenti di dialogo già esistenti, senza modificare il motore per far passare i casi della bozza.
- Provare visite precoci, domande sulle persone senza foto, frase e seguiti, ordine delle confessioni, giacca e accusa diretta, ritorni al don.
- Riportare esplicitamente omissioni di `dichiaro` o allucinazioni; non introdurre nuovi servizi per mascherarle.
- Alla futura applicazione sincronizzare soltanto i contenuti necessari nella copia Unity, senza ricompilazioni o modifiche al runtime non richieste.

## Incompatibilità ancora aperte

Foto iniziale e formula nel taccuino sono hardcoded. Il passaparola su Nino contiene la vecchia versione. Le consegne sono automatiche per scalino, non subordinate a domande libere. Gli scalini non controllano direttamente il possesso della giacca. Lo storico ha la finestra attuale. Questi limiti non sono risolti dai prompt e non sono autorizzazioni implicite a modificare codice.
