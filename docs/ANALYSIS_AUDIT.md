# Verifica di «Avvia analisi» — 16 ottobre 2026

Ambito: acquisizione degli input WPF, scansione, cache incrementale e salvataggio
SQLite. Le prove utilizzano esclusivamente cartelle e database temporanei.

## Problemi riprodotti e correzioni

| Problema | Effetto precedente | Correzione |
| --- | --- | --- |
| Modifica della stessa dimensione nello stesso secondo | L'hash precedente veniva riutilizzato anche con contenuto diverso. | Nuova colonna `DATA_FILE_MODIFICA_UTC_TICKS`, con la precisione UTC disponibile sul filesystem. Il confronto richiede anche questo valore. |
| Letture separate di attributi, metadati e hash | Il file poteva cambiare fra le letture; un file già in scrittura poteva essere saltato come invariato. | Apertura in lettura con `FileShare.Read`, mantenuta durante confronto incrementale, letture e salvataggio. Un writer già attivo fa fallire l'elaborazione e produce un errore nel log. |
| Percorsi ripetuti o sovrapposti | Stesso file contato ed elaborato più volte, statistiche gonfiate e possibili falsi duplicati con differenze di maiuscole. | Insiemi di percorsi visitati, distinti per file e cartelle, svuotati fra conteggio ed elaborazione e a ogni nuova scansione. |
| Database con un'estensione processabile | Un database chiamato, per esempio, `database.jpg` veniva trattato come foto. | Esclusione del database attivo e dei suoi file SQLite ausiliari dal conteggio e dall'analisi. |
| Input vuoti e reset con un file escluso | Apertura del database prima dell'errore; un reset poteva azzerarlo senza analizzare alcun file. | Normalizzazione e validazione degli input prima dell'apertura. Il reset rifiuta anche un singolo file con estensione non processabile o coincidente con il database. |
| Input modificati dopo l'avvio | Il worker WPF leggeva i campi dell'interfaccia dopo l'accodamento; l'array della richiesta rimaneva condiviso con il chiamante. | Acquisizione delle opzioni prima di pubblicare lo stato occupato, richiesta costruita sul thread UI e copia normalizzata dei percorsi nel servizio. |
| Richiamo diretto del comando mentre occupato | `ICommand.Execute` poteva avviare un'altra analisi senza consultare `CanExecute`. | Controllo di `IsBusy` anche all'ingresso dell'operazione. |

Il campo testuale della data resta invariato, così le chiavi derivate e i criteri
di ordinamento delle copie da conservare mantengono il formato precedente.
La nuova colonna viene aggiunta durante l'inizializzazione per l'analisi, senza
eliminare i record. I record precedenti partono da zero e vengono rianalizzati
una volta quando incontrati nella scansione. Questo rende la prima analisi dopo
l'aggiornamento più lenta; quelle successive tornano incrementali.

Un file nuovo che non può essere aperto stabilmente non viene inserito con dati
parziali. Il test sul precedente errore temporaneo di hash ora verifica il
rifiuto dell'apertura e il recupero alla scansione successiva. Un ulteriore test
verifica il recupero di un record con timestamp preciso ma hash mancante.
I record preesistenti di un file occupato restano disponibili come dati
dell'ultima analisi riuscita; la nuova analisi segnala il fallimento.

## Verifica

Prima delle correzioni fallivano tutti gli 11 casi iniziali di integrazione e i
due casi WPF. Sono stati aggiunti anche il test della migrazione del vecchio
schema e quello del recupero dell'hash mancante: 15 nuovi casi complessivi.

Il test di sicurezza della cancellazione con alias del percorso ora crea gli
alias in due scansioni successive: conserva la verifica delle protezioni in
presenza di vecchi record duplicati, mentre una singola scansione evita già
di inserire entrambe le varianti di maiuscole.

```powershell
dotnet test FotoCreaDB.sln --no-restore --verbosity minimal
dotnet build FotoCreaDB.sln --no-restore --no-incremental --warnaserror --verbosity minimal
```

Esito: 116 test superati (33 unitari, 70 di integrazione, 13 WPF), nessun test
ignorato; compilazione completa con zero avvisi ed errori.

## Limiti

- La cache usa dimensione e timestamp completo, senza rileggere il contenuto
  dei file invariati. Un programma che modifica i byte e ripristina esattamente
  dimensione e timestamp può ancora lasciare la cache obsoleta; anche un
  filesystem con date poco precise limita il rilevamento. La cancellazione
  continua a verificare hash e contenuto reale prima di eliminare una copia.
- Il blocco riguarda il singolo file durante l'elaborazione; un file può
  cambiare dopo la fine dell'analisi. Non è una fotografia atomica dell'intera
  raccolta. Gli errori lasciano gli aggiornamenti già completati nel database.
- La deduplicazione dei percorsi visitati vale per la singola scansione. Non
  bonifica alias già presenti nel database o inseriti in scansioni distinte e
  non identifica gli hard link durante l'analisi. Le protezioni della
  cancellazione sugli alias restano coperte dai test.
- Conteggio iniziale e scansione rimangono due passate; i file possono essere
  aggiunti o rimossi fra le passate. Questa verifica non è un benchmark di
  analisi completa di 150.000 fotografie.
- Restano la gestione prudente dei reparse point e la distinzione fra errori
  dei file e metadati non disponibili descritti nella [seconda revisione](SECOND_AUDIT.md).
