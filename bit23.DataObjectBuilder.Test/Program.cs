using System;
using System.Collections.Generic;
using System.Dynamic;
using Newtonsoft.Json.Linq;
using bit23;

namespace bit23.Examples
{
    public class OtherPersonClass
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Gender { get; set; }
    }


    class Program
    {
        static void PrintResult(string exampleName, IPersonDTO person)
        {
            Console.WriteLine($"{exampleName}:");
            Console.WriteLine();
            Console.WriteLine($"FirstName: {person.FirstName}");
            Console.WriteLine($"LastName: {person.LastName}");
            Console.WriteLine($"Gender: {person.Gender}");
            Console.WriteLine("-------------------------------------");
        }

        static void ExampleIDictionary()
        {
            var person = DataObjectBuilder.Default.Create<IPersonDTO>(new Dictionary<string, object>()
            {
                ["FirstName"] = "Riccardo",
                ["LastName"] = "Marzi",
                ["Gender"] = "Male",
            });

            PrintResult("IDictionary", person);
        }

        static void ExampleAnonymousObject()
        {
            var person = DataObjectBuilder.Default.Create<IPersonDTO>(new
            {
                FirstName = "Riccardo",
                LastName = "Marzi",
                Gender = "Male",
            });

            PrintResult("AnonymousObject", person);
        }

        static void ExampleExpandoObject()
        {
            dynamic data = new ExpandoObject();
            data.FirstName = "Riccardo";
            data.LastName = "Marzi";
            data.Gender = "Male";

            var person = DataObjectBuilder.Default.Create<IPersonDTO>(data);

            PrintResult("ExpandoObject", person);
        }

        static void ExampleJObject()
        {
            var data = new JObject();
            data.Add("FirstName", "Riccardo");
            data.Add("LastName", "Marzi");
            data.Add("Gender", "Male");

            var person = DataObjectBuilder.Default.Create<IPersonDTO>(data);

            PrintResult("JObject", person);
        }

        static void ExampleValueTuple()
        {
            (string FirstName, string LastName, string Gender) data = ("Riccardo", "Marzi", "Male");

            var person = DataObjectBuilder.Default.Create<IPersonDTO>(data);

            PrintResult("ValueTuple", person);
        }

        static void ExampleValueTupleWithNames()
        {
            (string FirstName, string LastName, string Gender) data = ("Riccardo", "Marzi", "Male");

            var person = DataObjectBuilder.Default.Create<IPersonDTO>(data, new[] { "FirstName", "LastName", "Gender" });

            PrintResult("ValueTupleWithNames", person);
        }

        static void ExampleTuple()
        {
            Tuple<string, string, string> data = new Tuple<string, string, string>("Riccardo", "Marzi", "Male");

            var person = DataObjectBuilder.Default.Create<IPersonDTO>(data);

            PrintResult("Tuple", person);
        }

        static void ExampleTupleWithNames()
        {
            Tuple<string, string, string> data = new Tuple<string, string, string>("Riccardo", "Marzi", "Male");

            var person = DataObjectBuilder.Default.Create<IPersonDTO>(data, new[] { "FirstName", "LastName", "Gender" });

            PrintResult("TupleWithNames", person);
        }

        static void ExampleObjectInstance()
        {
            var otherPerson = new OtherPersonClass()
            {
                FirstName = "Riccardo",
                LastName = "Marzi",
                Gender = "Male",
            };

            var person = DataObjectBuilder.Default.Create<IPersonDTO>(otherPerson);

            PrintResult("ObjectInstance", person);
        }


        static void ExampleFactory()
        {
            var otherPerson = new OtherPersonClass()
            {
                FirstName = "Riccardo",
                LastName = "Marzi",
                Gender = "Male",
            };

            var options = new DataObjectBuilderOptions<IPersonDTO>();
            //options.Mapping
            //    .Member(d => d.FirstName, "LastName")
            //    .Member(d => d.LastName, "Gender")
            //    .Member(d => d.Gender, "FirstName");
            //.Member(d => d.Gender, (Gender s) => s.ToString());
            options.TransformValue = (name, value) => $"***{value}***";

            var factory = DataObjectBuilder.Factory<IPersonDTO>(options);

            var person = factory.Create(new
            {
                FirstName = "Riccardo",
                LastName = "Marzi",
                Gender = "Male",
            });

            PrintResult("Factory", person);
        }


        static void Main(string[] args)
        {
            // ### IDictionary example ###
            ExampleIDictionary();

            // ### Anonymous Object example ###
            ExampleAnonymousObject();

            // ### Expando Object example ###
            ExampleExpandoObject();

            // ### JObject example ###
            ExampleJObject();

            // ### ValueTuple example ###
            ExampleValueTuple();
            ExampleValueTupleWithNames();

            // ### Tuple example ###
            ExampleTuple();
            ExampleTupleWithNames();

            // ### Object Instance example ###
            ExampleObjectInstance();

            // ### Factory example ###
            ExampleFactory();

            Console.ReadLine();
        }
    }
}
