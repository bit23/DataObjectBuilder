
# DataObjectBuilder

Defining it in the simplest way you can say that *DataObjectBuilder* allows the creation of "interface instances".  
The creation can be made from any instance of Object (which obviously does not implement the interface in question), an Anonymous Object, a JObject, a Tuple or ValueTuple, a Dictionary or a dynamic object.


## Introduction

Interfaces are, by construction, definitions of structured objects, empty and not instantiable, so in order to have an instance that adheres to this interface model, it is necessary to define a class that implements it and, subsequently, instantiate it in order to use.  
In many cases the defined class will implement the interface properties and nothing else, thus simply creating a data container. As a result, writing a class which only needs to display properties or fields, can be a tedious and unnecessarily time-consuming operation.  


## When is it useful?
A typical case in which it is possible to save time and number of files in the project can be found in the Data Transfer Object (DTO), whose task is only to host information to be transferred from one "system" to another. Using *DataObjectBuilder* it will be possible to manage the problem by defining only an interface that represents the DTO and, when generating an "interface instance", just call the  ```Create<TInterface>(source)``` method, where source can be any structured object: ```Object``` (with properties), ```AnonymousObject```, ```JObject```, ```Tuple```, ```ValueTuple```, ```IDictionary<string, object>```, ```ExpandoObject``` (dynamic).  
Another case may be where you need to "reduce" the number of information of a class by removing some properties from the object. Instead of creating a new reduced class or a hierarchy of classes and subclasses, you can define an interface with only the properties you want to expose and let the ``Create<TInterface>(source)`` method transfer the information from the class to the created "interface instance" for the properties defined in the interface itself.  
Obviously the cases are not limited to just these two, but they are certainly the most typical ones, which probably almost all developers have encountered in the course of their work.


## Advanced Features

...


## How it works?

So far we have talked about "interface instance" using quotes. This is necessary because as we know it is not possible to create interface instances.  
The .Net platform provides several classes in the ``System.Reflection.Emit`` namespace that deal with the dynamic creation of Assemblies, Classes and Types in general, Fields, Properties, Methods and any other construct present in the runtime.  
Thanks to these tools, the implementation of *DataObjectBuilder* makes it possible to dynamically generate a class that implements the interface passed as a generic argument and that, consequently, contains its properties. If the request for that interface has already been made previously, the class will have already been defined and will not be recreated.  
As soon as the target class is generated or retrieved, *DataObjectBuilder* will create an instance of the class and copy the values contained in the source object.  
Depending on the source object the values will be read by:
- properties, in case of: ```Object```, ```AnonymousObject```.
- defined properties, in the case of ``JObject``.
- entries, in the case of ``IDictionary<string, object>``, ``ExpandoObject``.
- fields, in case of ```Tuple```, ```ValueTuple```.

About ```ValueTuple``` it is necessary to remember that the field names defined in the source code are not exported during compilation and are in fact "syntactic sugar". Therefore in case of ```Tuple``` or ```ValueTuple``` as source object, unless a different mapping is specified, the values will be read in order (Item1, Item2, Item3, etc.) and copied on the properties of the target object in the order in which they are retrieved by reflection operations.


## Usage

*DataObjectBuilder* is a static class that displays the *Default* property, which returns the default Factory.
In this formulation the use becomes very simple and is limited only to the call:

```csharp
IMyInterface result = DataObjectBuilder.Default.Create<IMyInterface>(source);
```

> see examples section

If you want a different configuration from the default one, such as more control in case of missing properties and mismatched types, or if you want to specify a custom mapping or apply transformations, you can create a Factory with options that respond to these cases and define custom operations:

```csharp
var options = new DataObjectBuilderOptions<IMyInterface>();
// configure options

var factory = DataObjectBuilder.Factory<IMyInterface>(options);

...

IMyInterface result = factory.Create(source);
```

> see examples section

### DataObjectBuilderOptions

The *DataObjectBuilderOptions* object allows you to specify how to manage the data transfer operation. Through these options you can set the following behaviors:
- Exception in case the source object does not contain one of the properties of the target object. If not specified this error is ignored.  
```options.ThrowOnMissingSourceMember = true;```
- Exception in case the property of the source object is a type that cannot be assigned to the corresponding property of the target object. If not specified, this error is ignored.  
```options.ThrowOnInvalidSourceMemberType = true;```.
- Custom retrieval of the values from the source object according to the expression set in the property ```Expression<Func<object, IDictionary<string, object>>> ReadSourceProperties { get; set; }```, which, using the source object passed in the lamba function, must return a key/value dictionary representing the properties.
- Mapping one or more properties of the source object to the properties of the target object. If desired, it can be specified only for the properties that need it.  
```options.Mapping.Member(d => d.MyProp1, "Prop1")```
- Final transformation of the values read by the source object according to the expression set in the property ```Expression<Func<string, object, object>> TransformValue { get; set; }```, which will receive as input the name of the current member and its value and must return the new transformed value according to the defined criteria.


## Examples

...