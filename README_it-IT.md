
# DataObjectBuilder

Definendolo nella maniera più semplice è possibile dire che *DataObjectBuilder* permette la creazione di "istanze di interfacce".  
La creazione può avvenire a partire da un'istanza di un oggetto qualunque (che ovviamente non implementi l'interfaccia in questione), da un Oggetto Anonimo, da un JObject, da una Tuple o ValueTuple, da un Dizionario o da un oggetto dinamico.


## Premessa

Le interfacce sono per costruzione delle definizioni di oggetti strutturati, vuoti e non istanziabili, pertanto per poter disporre di una istanza che aderisca a tale modello di interfaccia, è necessario definire una classe che la implementi e successivamente istanziarla per poterla utilizzare.  
In molti casi la classe definita implementarà le proprietà dell'interfaccia e niente altro, creando così semplicemente un contenitore di dati. Ne risulta che la scrittura di una classe, che deve solo esporre proprietà o campi, può essere un'operazione noiosa e inutilmente costosa in termini di tempo.  


## Quando è utile?

Un caso tipico nel quale è possibile risparmiare tempo e numero di file nel progetto può essere trovato nei Data Transfer Object (DTO), il cui compito è solo quello di ospitare informazioni da trasferire da un "sistema" ad un altro. Utilizzando *DataObjectBuilder* sarà possibile gestire il problema definendo solo un'interfaccia che rappresenti il DTO e al momento di generare un'"istanza di interfaccia" basterà chiamare il metodo ```Create<TInterface>(source)```, dove source può essere un qualunque oggetto strutturato: ```Object``` (con proprietà),  ```AnonymousObject```, ```JObject```, ```Tuple```, ```ValueTuple```, ```IDictionary<string, object>```, ```ExpandoObject``` (dynamic).  
Un altro caso può essere quello in cui si ha la necessità di "ridurre" il numero di informazioni di una classe rimuovendo alcune proprietà dall'oggetto. Invece di creare una nuova classe ridotta o una gerarchia di classi e sottoclassi, è possibile definire un'interfaccia con le sole proprietà che si vogliono esporre e lasciare che il metodo ```Create<TInterface>(source)``` si occupi di trasferire le informazioni dalla classe all'"istanza di interfaccia" creata, per le proprietà definite nell'interfaccia stessa.  
Ovviamente i casi non si limitano a solo questi due, ma sono sicuramente i più tipici, che probabilmente quasi tutti gli sviluppatori hanno incontrato nel corso del proprio lavoro.


## Funzionalità avanzate

...


## Come funziona?

Finora abbiamo parlato di "istanza di interfaccia" utilizzando le virgolette. Questo è necessario perchè come ben sappiamo non è possibile creare istanze di interfacce.  
La piattaforma .Net mette a disposizione svariate classi nel namespace ```System.Reflection.Emit``` che si occupano della creazione dinamica di Assemblies, Classi e tipi in generale, Campi, Proprietà, Metodi e qualunque altro costrutto presente nel runtime.  
Grazie a questi strumenti l'implementazione di *DataObjectBuilder* fa si che al momento della chiamata al metodo ```Create<TInterface>(source)```, dinamicamente si generi una classe che implementa l'interfaccia passata come argomento generico e che, di conseguenza, ne contenga le proprietà. Se la richiesta per quell'interfaccia è già stata fatta in precedenza la classe sarà già stata definita e non verrà ricreata.  
Appena generata o recuperata la classe di destinazione, *DataObjectBuilder* si occuperà di creare un'istanza della classe e di copiarci i valori contenuti nell'oggetto source.  
A seconda dell'oggetto sorgente i valori verranno letti da:
- proprietà, nel caso di: ```Object```, ```AnonymousObject```
- proprietà definite, nel caso di ```JObject```
- entries, nel caso di ```IDictionary<string, object>```, ```ExpandoObject```
- campi, nel caso di ```Tuple```, ```ValueTuple```

Riguardo a ```ValueTuple``` è necessario ricordare che i nomi dei campi definiti nel codice sorgente non vengono esportati durante la compilazione e sono di fatto "syntactic sugar". Pertanto in caso di ```Tuple``` o ```ValueTuple``` come oggetto sorgente, a meno che non venga specificato un mapping diverso, i valori verranno letti in ordine (Item1, Item2, Item3, ecc.) e copiati sulle proprietà dell'oggetto di destinazione nell'ordine in cui vengono recurate dalle operazioni di reflection.


## Utilizzo

*DataObjectBuilder* si presenta come un classe statica che espone la proprietà *Default*, la quale restituisce appunto la Factory di default.
In questa formulazione l'uso diventa molto semplice e si limita alla sola chiamata:
```csharp
IMyInterface result = DataObjectBuilder.Default.Create<IMyInterface>(source);
```
> vedi la sezione esempi

Se si desidera una configurazione diversa da quella di default, come ad esempio un maggiore controllo nel caso di proprietà mancanti e tipi non corrispondenti o se si volesse specificare un mapping personalizzato o, ancora, applicare delle trasformazioni, è possibile creare una Factory con delle opzioni che rispondano a queste casistiche e definiscano le operazioni personalizzate:

```csharp
var options = new DataObjectBuilderOptions<IMyInterface>();
// configure options

var factory = DataObjectBuilder.Factory<IMyInterface>(options);

...

IMyInterface result = factory.Create(source);
```
> vedi la sezione esempi

### DataObjectBuilderOptions

L'oggetto *DataObjectBuilderOptions* permette di specificare come gestire l'operazione di trasferimento dei dati. Tramite queste opzioni è possibile definire i seguenti comportamenti:
- Eccezione nel caso l'oggetto sorgente non contenga una delle proprietà dell'oggetto di destinazione. Se non specificato questo errore viene ignorato.  
```options.ThrowOnMissingSourceMember = true;```
- Eccezione nel caso la proprietà dell'oggetto sorgente sia di un tipo non assegnabile alla corrispondente proprietà dell'oggetto di destinazione. Se non specificato questo errore viene ignorato.  
```options.ThrowOnInvalidSourceMemberType = true;```
- Recupero personalizzato dei valori dall'oggetto sorgente in base all'espressione impostata nella proprietà ```Expression<Func<object, IDictionary<string, object>>> ReadSourceProperties { get; set; }```, la quale, utilizzando l'oggetto source passato nella lamba function, dovrà restituire un dizionario di chiave/valore rappresentante le proprietà.
- Mapping di una o più proprietà dell'oggetto sorgente sulle proprietà dell'oggetto di destinazione. Volendo può essere specificato solo per le proprietà che ne necessitano.  
```options.Mapping.Member(d => d.MyProp1, "Prop1")```
- Trasformazione finale dei valori letti dall'oggetto sorgente in base all'espressione impostata nella proprietà ```Expression<Func<string, object, object>> TransformValue { get; set; }```, la quale riceverà in input il nome del membro corrente ed il suo valore e dovrà restituire il nuovo valore trasformato in base al criterio definito.


## Esempi

...