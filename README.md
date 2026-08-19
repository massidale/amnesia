# Amnesia

San Rocco di Valdieri, ottobre 1987. Un uomo si sveglia da due anni di coma con
tre oggetti in mano, una frase che non significa niente e un nome che non sa a
chi appartenga.

Gioco d'indagine sociale in cui **la conversazione e' la meccanica**: i
personaggi sono modelli linguistici, e l'unico verbo del giocatore e' parlare,
mostrare una cosa, e accostare due cose che sono state dette.

## Struttura

| | |
|---|---|
| `Amnesia.Domain/` | il dominio: logica pura, `netstandard2.1`, nessuna dipendenza da un engine |
| `Amnesia.Domain.Tests/` | NUnit — **girano senza aprire Unity** |
| `Unity/` | la presentazione (3D a bassa risoluzione), consuma il dominio |
| `docs/specs/` | storia, design, contenuto |
| `docs/plans/` | i piani eseguibili |

Il dominio non sa niente di come il gioco venga disegnato, e non deve saperlo:
e' la ragione per cui esiste come progetto separato invece che dentro `Assets/`.

## Test

```
dotnet test
```

Nessun test tocca la rete. La chiave OpenRouter vive in `.env`, che e'
gitignorato e non va mai stampato, registrato o incluso in un messaggio d'errore.

## Da dove viene

Fork di *Valnera* (Godot/GDScript): stessa architettura, storia sostituita,
livello mercantile e stato sociale numerico rimossi. Il motore precedente resta
su `videogioco`, branch `main`.
