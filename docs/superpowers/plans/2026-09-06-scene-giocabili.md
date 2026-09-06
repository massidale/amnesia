# Scene giocabili e Teresa in piazza

Perimetro approvato: riusare le schede e il motore esistenti; niente nuove relazioni.

1. Aggiungere una verifica degli asset salvati per Teresa in piazza e Bootstrap/Giocatore nelle due scene. Verificare il fallimento prima delle modifiche.
2. Ancorare Teresa alla prima seduta della piazza e aggiornare anteprima e controlli spaziali.
3. Generare entrambe le scene con Bootstrap in modalita mappa a mano, Giocatore, Pannello, Menu e ponte dei luoghi. Conservare la generazione visita libera come opzione distinta.
4. Adattare la corriera ai due controller. Trasferire la sessione narrativa in memoria solo durante il viaggio, ripristinare le porte gia aperte, non creare duplicati persistenti.
5. Verificare in Play registro NPC, saluti locali, porte e viaggio reciproco con stato invariato salvo il tempo di viaggio. Nessun test deve stampare credenziali o richiedere chiamate LLM.
6. Rigenerare tramite Unity aperto, eseguire controlli Node e ispezionare Teresa e interfaccia. Documentare controlli e limiti, compresa l'assenza di un nuovo salvataggio su disco.
