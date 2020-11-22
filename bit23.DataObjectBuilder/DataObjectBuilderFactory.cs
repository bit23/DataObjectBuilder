using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;

namespace bit23
{
    public class DataObjectBuilderFactory
    {
        private DataObjectBuilderOptions options;

        internal DataObjectBuilderFactory(DataObjectBuilderOptions options = null)
        {
            this.options = options;
        }


        public TInterface Create<TInterface>(object source)
        {
            return DataObjectBuilder.BuildInterfaceClass<TInterface>(source, null, this.options);
        }

        public TInterface Create<TInterface>(ITuple source, IEnumerable<string> elementNames = null)
        {
            return DataObjectBuilder.BuildInterfaceClass<TInterface>(source, elementNames?.ToArray(), this.options);
        }


        public IEnumerable<TInterface> CreateSequence<TInterface>(IEnumerable sourceSequence)
        {
            // TODO: check sourceSequence

            var result = new List<TInterface>();
            foreach (var source in sourceSequence)
            {
                result.Add(
                    DataObjectBuilder.BuildInterfaceClass<TInterface>(source, null, this.options)
                    );
            }
            return result;
        }

        public IEnumerable<TInterface> CreateSequence<TInterface, TSource>(IEnumerable<TSource> sourceSequence)
        {
            var result = new List<TInterface>();
            foreach (var source in sourceSequence)
            {
                result.Add(
                    DataObjectBuilder.BuildInterfaceClass<TInterface>(source, null, this.options)
                    );
            }
            return result;
        }

        public IEnumerable<TInterface> CreateSequence<TInterface, TSource>(IEnumerable<TSource> sourceSequence, IEnumerable<string> elementNames = null)
            where TSource: ITuple
        {
            var result = new List<TInterface>();
            var elementNamesArray = elementNames?.ToArray();

            foreach (var source in sourceSequence)
            {
                result.Add(
                    DataObjectBuilder.BuildInterfaceClass<TInterface>(source, elementNamesArray, this.options)
                    );
            }
            return result;
        }
    }

    public class DataObjectBuilderFactory<TInterface>
    {
        private DataObjectBuilderOptions<TInterface> options;
        private Type interfaceType;

        internal DataObjectBuilderFactory(DataObjectBuilderOptions<TInterface> options = null)
        {
            var itype = typeof(TInterface);
            if (!itype.IsInterface)
                throw new ArgumentException($"Type {itype.Name} must be an interface.");

            this.interfaceType = itype;
            this.options = options;
        }

        public TInterface Create(object source)
        {
            return DataObjectBuilder.BuildInterfaceClass<TInterface>(source, null, this.options);
        }

        public TInterface Create(ITuple source, IEnumerable<string> elementNames = null)
        {
            return DataObjectBuilder.BuildInterfaceClass<TInterface>(source, elementNames?.ToArray(), this.options);
        }


        public IEnumerable<TInterface> CreateSequence<TSource>(IEnumerable<TSource> sourceSequence)
        {
            var result = new List<TInterface>();
            foreach (var source in sourceSequence)
            {
                result.Add(
                    DataObjectBuilder.BuildInterfaceClass<TInterface>(source, null, this.options)
                    );
            }
            return result;
        }

        public IEnumerable<TInterface> CreateSequence<TSource>(IEnumerable<TSource> sourceSequence, IEnumerable<string> elementNames = null)
            where TSource : ITuple
        {
            var result = new List<TInterface>();
            var elementNamesArray = elementNames?.ToArray();

            foreach (var source in sourceSequence)
            {
                result.Add(
                    DataObjectBuilder.BuildInterfaceClass<TInterface>(source, elementNamesArray, this.options)
                    );
            }
            return result;
        }
    }
}
