# Verifica di «Genera report duplicati»

## Problemi corretti

- Un percorso errato poteva creare un nuovo database vuoto e mostrare zero
  duplicati. Il report ora apre esclusivamente database esistenti, in sola
  lettura, senza creare cartelle, tabelle o indici.
- Database vuoti, estranei o con uno schema precedente vengono rifiutati senza
  modificarli. Per aggiornare un vecchio database FotoCreaDB occorre eseguire
  l'analisi, che mantiene la responsabilità delle migrazioni.
- I conteggi precedenti potevano restare visibili dopo un errore o un cambio di
  database. Ora il risultato sconosciuto è «—»; un report valido senza duplicati
  continua a mostrare zero. Analisi, cancellazione e rimozione del database
  invalidano il riepilogo precedente.
- Percorso e opzione dettagli vengono acquisiti prima di avviare il lavoro.
  I campi dei percorsi sono in sola lettura durante le operazioni; un risultato
  appartenente a un database diverso da quello selezionato non viene pubblicato.
- Il completamento arrivava prima della scrittura dei dettagli. Ora viene
  comunicato alla fine; durante lettura e preparazione la barra è indeterminata.
  Gli errori del callback di log non vengono più ignorati dichiarando successo.
- Il raggruppamento confrontava ogni candidato con tutti i membri del gruppo,
  con costo quadratico. Ora usa indici per dimensione e data di scatto,
  conservando le regole di compatibilità, l'ordine dei gruppi e la scelta della
  copia da tenere. L'ordinamento dei candidati resta necessario.

## Verifica

I nuovi test coprono database mancanti, vuoti, estranei e precedenti, blocco
delle scritture, contenuto del database invariato, ordine di completamento,
errori di log, risultati obsoleti nella GUI e distinzione fra zero e risultato
sconosciuto. Tutti i database sono temporanei; nessuna foto reale viene usata.

Un test raggruppa 150.000 candidati sintetici identici; nella prova locale il
solo raggruppamento ha richiesto circa 65 ms. È una verifica dell'algoritmo,
senza lettura SQLite o accesso alle fotografie. Altri 300 insiemi deterministici
vengono confrontati con una semplice implementazione di riferimento, includendo
date mancanti, date discordanti, dimensioni diverse e spareggi nella scelta
della copia da conservare.

```powershell
dotnet test FotoCreaDB.sln --no-restore --verbosity minimal
dotnet build FotoCreaDB.sln --no-restore --no-incremental --warnaserror --verbosity minimal
```

Esito: 101 test superati (33 unitari, 57 di integrazione, 11 WPF), compilazione
con zero avvisi ed errori.

## Significato del report

Il report descrive i candidati risultanti dall'ultima analisi salvata nel
database. Non rilegge le foto per verificare che esistano ancora o che il loro
contenuto sia rimasto invariato. File modificati o rimossi dopo l'analisi possono
quindi comparire nel riepilogo. La cancellazione mantiene le verifiche sui file
reali prima di eliminare una copia.

L'elenco dettagliato può produrre molte righe di log; su raccolte grandi conviene
lasciare disattivata la relativa casella quando bastano conteggi e spazio.
