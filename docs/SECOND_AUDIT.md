# Seconda ricerca di bug — 16 ottobre 2026

Ambito: scansione incrementale, coerenza degli stati SQLite, validazione degli
input prima del reset e collegamenti del filesystem. Nessuna prova ha utilizzato
foto o database dell'utente: tutti i dati sono stati creati in cartelle temporanee.

## Risultati e correzioni

| Problema riprodotto | Effetto precedente | Correzione |
| --- | --- | --- |
| Radici con `%` o `_` nella query SQL `LIKE` | La bonifica coinvolgeva anche cartelle con nomi diversi. | Escape esplicito dei caratteri speciali e del carattere di escape. |
| Radice vuota nella bonifica | Il prefisso `%` poteva coinvolgere tutti i record non visitati. | Rifiuto della radice vuota e di un token di scansione vuoto. |
| File non visitato ma ancora esistente | `FILE_ESISTE` veniva impostato a zero anche per file esclusi o non raggiunti durante una scansione parziale. | La bonifica controlla l'assenza sul filesystem; un errore di accesso non è una conferma di assenza. Una radice non accessibile non viene bonificata. |
| Errore temporaneo di hash | Il file rimaneva senza hash anche alle scansioni successive, perché data e dimensione erano invariate. | Il salto incrementale richiede un hash SHA-256 valido e un record presente. I file senza hash e quelli ricomparsi vengono rielaborati. Le note di errore vengono conservate. |
| Junction e directory collegate | La scansione usciva dalla cartella scelta e poteva seguire cicli di cartelle. | Il conteggio e la scansione ignorano directory reparse e file reparse. Le directory collegate producono un avviso. |
| Input vuoto o non valido con reset database | Il database poteva essere cancellato prima che lo scanner rilevasse l'input errato. | Validazione prima dell'apertura/reset. Per il reset vengono rifiutati anche percorsi mancanti, non accessibili e collegamenti ignorati dallo scanner. |
| Errori presentati come successo | Un percorso inesistente produceva comunque `Success = true` e «Analisi completata». | Contatore degli errori dei percorsi e risultato negativo per errori di percorso, file, cartella, hash o database; messaggio esplicito nel riepilogo. La sola indisponibilità di metadati non rende fallita l'analisi binaria. |
| Riutilizzo della stessa istanza dello scanner | Il token veniva riutilizzato: una foto rimossa tra due scansioni non veniva segnata come mancante. | Nuovo token per ogni chiamata a `Scan`. |

Nell'interfaccia, una cancellazione del database rifiutata viene ora indicata come
«Database non eliminato. Consultare il log», evitando di dichiarare erroneamente
che il database non esiste.

## Prove

I primi **9 casi di regressione fallivano tutti** prima delle correzioni. Sono stati
aggiunti anche controlli per ciclo di junction, reset con percorso inesistente o
collegato e riutilizzo dello scanner. Il test del percorso troppo lungo ora
verifica che l'analisi termini senza crash e segnali correttamente il fallimento.

Comandi di verifica:

```powershell
dotnet test FotoCreaDB.sln --no-restore --verbosity minimal
dotnet build FotoCreaDB.sln --no-restore --no-incremental --warnaserror --verbosity minimal
```

Esito: **73 test superati** (30 unitari, 43 di integrazione), nessun test fallito
o ignorato; **zero avvisi e zero errori** nella compilazione completa con gli
avvisi trattati come errori. I 13 casi aggiunti si trovano in
`FotoCreaDB.Tests.Integration/Analysis/IncrementalScanRegressionTests.cs`.

## Limiti

- La scansione incrementale continua a usare dimensione e data di modifica al
  secondo per riutilizzare un hash già valido. Modifiche della stessa dimensione
  con la stessa data possono lasciare un report non aggiornato. Il servizio di
  cancellazione verifica nuovamente hash e byte dei file sotto blocco: il solo
  report non autorizza una cancellazione.
- Ignorare i collegamenti è una scelta prudente: una collezione raggiungibile
  soltanto tramite junction/link va analizzata dal suo percorso reale. Anche
  alcuni placeholder cloud sono file reparse e vengono ignorati.
- Gli stati di presenza nel database sono osservazioni, non una transazione con
  il filesystem: un file può cambiare dopo la scansione. Restano necessari i
  controlli effettuati durante la cancellazione.
- La bonifica fa letture del filesystem aggiuntive per i record non visitati.
- Restano le limitazioni della [prima revisione](SAFETY_AUDIT.md), inclusa la
  cancellazione definitiva senza Cestino/quarantena.
