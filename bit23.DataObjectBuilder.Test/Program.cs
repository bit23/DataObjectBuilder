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
        static void PrintPerson(IPersonDTO person)
        {
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

            PrintPerson(person);
        }

        static void ExampleAnonymousObject()
        {
            var person = DataObjectBuilder.Default.Create<IPersonDTO>(new
            {
                FirstName = "Riccardo",
                LastName = "Marzi",
                Gender = "Male",
            });

            PrintPerson(person);
        }

        static void ExampleExpandoObject()
        {
            dynamic data = new ExpandoObject();
            data.FirstName = "Riccardo";
            data.LastName = "Marzi";
            data.Gender = "Male";

            var person = DataObjectBuilder.Default.Create<IPersonDTO>(data);

            PrintPerson(person);
        }

        static void ExampleJObject()
        {
            var data = new JObject();
            data.Add("FirstName", "Riccardo");
            data.Add("LastName", "Marzi");
            data.Add("Gender", "Male");

            var person = DataObjectBuilder.Default.Create<IPersonDTO>(data);

            PrintPerson(person);
        }

        static void ExampleValueTuple()
        {
            (string FirstName, string LastName, string Gender) data = ("Riccardo", "Marzi", "Male");

            var person = DataObjectBuilder.Default.Create<IPersonDTO>(data);

            PrintPerson(person);
        }

        static void ExampleValueTupleWithNames()
        {
            (string FirstName, string LastName, string Gender) data = ("Riccardo", "Marzi", "Male");

            var person = DataObjectBuilder.Default.Create<IPersonDTO>(data, new[] { "FirstName", "LastName", "Gender" });

            PrintPerson(person);
        }

        static void ExampleTuple()
        {
            Tuple<string, string, string> data = new Tuple<string, string, string>("Riccardo", "Marzi", "Male");

            var person = DataObjectBuilder.Default.Create<IPersonDTO>(data);

            PrintPerson(person);
        }

        static void ExampleTupleWithNames()
        {
            Tuple<string, string, string> data = new Tuple<string, string, string>("Riccardo", "Marzi", "Male");

            var person = DataObjectBuilder.Default.Create<IPersonDTO>(data, new[] { "FirstName", "LastName", "Gender" });

            PrintPerson(person);
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

            PrintPerson(person);
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

            PrintPerson(person);
        }


        static void Main(string[] args)
        {
            // ### Factory example ###
            ExampleFactory();

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

            Console.ReadLine();
        }
    }
}
