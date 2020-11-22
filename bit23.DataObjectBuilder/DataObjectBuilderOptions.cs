using System;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace bit23
{
    public class DataObjectBuilderOptions
    {
        private Func<object, IDictionary<string, object>> readSourcePropertiesFunc;
        private Func<string, object, object> transformValueFunc;


        internal Func<object, IDictionary<string, object>> ReadSourcePropertiesFunc
        {
            get
            {
                if (this.readSourcePropertiesFunc == null)
                    this.readSourcePropertiesFunc = this.ReadSourceProperties.Compile();
                return this.readSourcePropertiesFunc;
            }
        }

        internal Func<string, object, object> TransformValueFunc
        {
            get
            {
                if (this.transformValueFunc == null)
                    this.transformValueFunc = this.TransformValue.Compile();
                return this.transformValueFunc;
            }
        }


        public virtual bool IsGeneric => false;


        public Expression<Func<object, IDictionary<string, object>>> ReadSourceProperties { get; set; }

        public Expression<Func<string, object, object>> TransformValue { get; set; }

        public bool ThrowOnMissingSourceMember { get; set; }

        public bool ThrowOnInvalidSourceMemberType { get; set; }
    }

    public class DataObjectBuilderOptions<TDestination> : DataObjectBuilderOptions
    {
        private MapMembersExpression<TDestination> destSourceMapExp;

        private void EnsureMapMemberExpressionExists()
        {
            if (destSourceMapExp == null)
                destSourceMapExp = new MapMembersExpression<TDestination>();
        }


        public override bool IsGeneric => true;

        public MapMembersExpression<TDestination> Mapping
        {
            get
            {
                this.EnsureMapMemberExpressionExists();
                return destSourceMapExp;
            }
        }
    }


    internal abstract class MemberMappingInfo
    {
        public abstract MemberInfo Destination { get; }
        public abstract Type DestinationType { get; }
        public abstract Type SourceType { get; }
        public abstract string DestinationName { get; }

        public abstract string SourceName { get; }

        public abstract Func<object, MemberInfo> SourceFunc { get;}
    }

    internal class MemberMappingInfo<TDestination, TSource> : MemberMappingInfo
    {
        private MemberInfo destination;
        private Func<object, MemberInfo> sourceFunc;
        private string sourceName;

        public MemberMappingInfo(MemberInfo destination, Func<object, MemberInfo> sourceFunc, string sourceMemberName)
        {
            this.destination = destination;
            this.sourceFunc = sourceFunc;
            this.sourceName = sourceMemberName;
        }



        //public Func<TSource, MemberInfo> SourceFunc { get; set; }
        public override Func<object, MemberInfo> SourceFunc => this.sourceFunc;

        public override MemberInfo Destination => this.destination;

        public override string DestinationName => this.Destination.Name;

        public override Type DestinationType => typeof(TDestination);

        public override Type SourceType => typeof(TSource);

        public override string SourceName => this.sourceName;
    }


    public class MapMembersExpression<TDestination>
    {
        private static MemberInfo FindMemberInfo(LambdaExpression lambdaExpression)
        {
            Expression expressionToCheck = lambdaExpression;

            var done = false;

            while (!done)
            {
                switch (expressionToCheck.NodeType)
                {
                    case ExpressionType.Convert:
                        expressionToCheck = ((UnaryExpression)expressionToCheck).Operand;
                        break;
                    case ExpressionType.Lambda:
                        expressionToCheck = ((LambdaExpression)expressionToCheck).Body;
                        break;
                    case ExpressionType.MemberAccess:
                        var memberExpression = ((MemberExpression)expressionToCheck);

                        if (memberExpression.Expression.NodeType != ExpressionType.Parameter &&
                            memberExpression.Expression.NodeType != ExpressionType.Convert)
                        {
                            throw new ArgumentException(
                                $"Expression '{lambdaExpression}' must resolve to top-level member and not any child object's properties. You can use ForPath, a custom resolver on the child type or the AfterMap option instead.",
                                nameof(lambdaExpression));
                        }

                        var member = memberExpression.Member;
                        return member;

                    default:
                        done = true;
                        break;
                }
            }

            throw new Exception(
                "Custom configuration for members is only supported for top-level individual members on a type.");
        }

        private static Func<object, MemberInfo> FindMemberInfoFunc(LambdaExpression lambdaExpression)
        {
            var member = FindMemberInfo(lambdaExpression);
            return new Func<object, MemberInfo>((source) => member);
        }

        private static Func<object, MemberInfo> FindMemberInfoFunc(string name)
        {
            return new Func<object, MemberInfo>(source =>
            {
                if (source == null)
                    return null;
                var sourceType = source.GetType();
                var property = sourceType.GetTypeInfo().DeclaredProperties.SingleOrDefault(x => x.Name == name);
                return property;
            });
        }


        private Dictionary<MemberInfo, Dictionary<Type, MemberMappingInfo>> membersMappingRegistry = new Dictionary<MemberInfo, Dictionary<Type, MemberMappingInfo>>();


        private void AddMemberMapping(MemberMappingInfo mappingInfo)
        {
            if (membersMappingRegistry.ContainsKey(mappingInfo.Destination))
            {
                var memberMappings = membersMappingRegistry[mappingInfo.Destination];
                if (memberMappings.ContainsKey(mappingInfo.SourceType))
                {
                    memberMappings[mappingInfo.SourceType] = mappingInfo;
                }
                else
                {
                    memberMappings.Add(mappingInfo.SourceType, mappingInfo);
                }
            }
            else
            {
                var memberMappings = new Dictionary<Type, MemberMappingInfo>();
                memberMappings.Add(mappingInfo.SourceType, mappingInfo);
                membersMappingRegistry.Add(mappingInfo.Destination, memberMappings);
            }
        }

        private void CopyValue(object source, MemberInfo sourceMemberInfo, TDestination destination, MemberInfo destinationMember)
        {
            object sourceValue = null;
            if (sourceMemberInfo.MemberType == MemberTypes.Field)
            {
                sourceValue = (sourceMemberInfo as FieldInfo).GetValue(source);
            }
            else if (sourceMemberInfo.MemberType == MemberTypes.Property)
            {
                sourceValue = (sourceMemberInfo as PropertyInfo).GetMethod.Invoke(source, null);
            }
            else
            {
                throw new ApplicationException();
            }

            if (destinationMember.MemberType == MemberTypes.Field)
            {
                (destinationMember as FieldInfo).SetValue(destination, sourceValue);
            }
            else if (destinationMember.MemberType == MemberTypes.Property)
            {
                (destinationMember as PropertyInfo).SetMethod.Invoke(source, new object[] { sourceValue });
            }
            else
            {
                throw new ApplicationException();
            }
        }


        internal void Map(object source, TDestination destination)
        {
            var sourceType = source.GetType();

            foreach(var map in membersMappingRegistry)
            {
                MemberMappingInfo mappingInfo = null;

                if (map.Value.ContainsKey(sourceType))
                {
                    mappingInfo = map.Value[sourceType];
                }
                else if (map.Value.ContainsKey(typeof(object)))
                {
                    mappingInfo = map.Value[sourceType];
                }
                else
                {
                    throw new Exception($"missing mapper for source type '{sourceType.Name}' or source type 'object'");
                }

                var sourceMember = mappingInfo.SourceFunc(source);
                this.CopyValue(source, sourceMember, destination, mappingInfo.Destination);
            }
        }


        private static string FindSourceMemberName<TSource, TMember>(Expression<Func<TSource, TMember>> expression)
        {
            throw new NotImplementedException();
        }

        public MapMembersExpression<TDestination> Member<TMember, TSource>(
            Expression<Func<TDestination, TMember>> destinationMember,
            Expression<Func<TSource, TMember>> sourceMember)
        {
            var destinationMemberInfo = FindMemberInfo(destinationMember);
            var sourceMemberInfo = FindMemberInfoFunc(sourceMember);

            var sourceMemberName = FindSourceMemberName(sourceMember);

            var mappingInfo = new MemberMappingInfo<TDestination, TSource>(destinationMemberInfo, sourceMemberInfo, sourceMemberName);
            this.AddMemberMapping(mappingInfo);
            return this;
        }

        public MapMembersExpression<TDestination> Member<TMember>(
            Expression<Func<TDestination, TMember>> destinationMember,
            string sourceMemberName)
        {
            var destinationMemberInfo = FindMemberInfo(destinationMember);
            var sourceMemberInfo = FindMemberInfoFunc(sourceMemberName);

            var mappingInfo = new MemberMappingInfo<TDestination, object>(destinationMemberInfo, sourceMemberInfo, sourceMemberName);
            this.AddMemberMapping(mappingInfo);
            return this;
        }
    }
}
