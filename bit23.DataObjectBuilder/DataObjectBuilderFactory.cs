﻿using System;
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
    }
}