using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace ReadingYamlFile
{
    public class Program
    {
        private const string CurrentEnvironment = "dev";
        
        public static void Main()
        {
            // Solution 1 - Read all yml file
            ReadYaml();

            // Solution 2 - Read a value from multiple nodes
            //ReadValueFromMultipleNodes();
        }

        private static void ReadYaml()
        {
            var document = File.ReadAllText(
                @"/Users/ingrid.santos/Documents/Personal/ReadingYamlFile/ReadingYamlFile/YmlRl.yml");
            
            // Setup the input
            var input = new StringReader(document);

            // Load the stream
            var yaml = new YamlStream();
            yaml.Load(input);

            // Examine the stream
            var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;
            
            // List all the items
            foreach (var (key, value) in mapping.Children)
            {
                var ymlKeyValue = ReadLeaf(value, ((YamlScalarNode)key).Value);
                
                Console.WriteLine($"Count {ymlKeyValue.Count()}");
            }
        }

        private static void ReadValueFromMultipleNodes()
        {
            object yamlObject;

            var text = System.IO.File.ReadAllText(
                @"/Users/ingrid.santos/Documents/Personal/ReadingYamlFile/ReadingYamlFile/YmlSample.yml");

            using (var sr = new StringReader(text))
                yamlObject = new Deserializer().Deserialize(sr);

            var data = new YamlValueReader(yamlObject)
                .On("node1")
                .On("node2")
                .On("node3")
                .On("node4")
                .On("value")
                .On("dev")
                .Get("default");

            Console.WriteLine("Yml value on default node " + data);
        }
        
        private class YamlValueReader
        {
            private readonly object _yamlDictionary;
            private string _key;
            private object _current;

            public YamlValueReader(object yamlDic)
            {
                _yamlDictionary = yamlDic;
            }

            public YamlValueReader On(string key)
            {
                _key = key;
                _current = Query<object>(_current ?? _yamlDictionary, _key, null);
                return this;
            }

            public string Get(string property)
            {
                if (_current == null)
                    throw new InvalidOperationException();

                _current = Query<object>(_current, null, property, _key);
                
                var strArray = ((_current as List<object>) ?? throw new Exception("Attribute not found.")).FirstOrDefault();
                
                return ((strArray as List<object>) ?? throw new Exception("Attribute not found.")).FirstOrDefault()?.ToString();
            }
            
            private static IEnumerable<T> Query<T>(object dictionary, string key, string propertyName, string fromKey = null)
            {
                var result = new List<T>();
                
                switch (dictionary)
                {
                    case null:
                        return result;
                    case IDictionary<object, object> dic:
                    {
                        var d = dic.Cast<KeyValuePair<object, object>>();

                        foreach (var (obj, val) in d)
                        {
                            if (obj as string == key)
                            {
                                if (propertyName == null)
                                { 
                                    result.Add((T)val);
                                }
                            }
                            else if (fromKey == key && obj as string == propertyName)
                            { 
                                result.Add((T)val);
                            }
                            else
                            { 
                                result.AddRange(Query<T>(val, key, propertyName, obj as string));
                            }
                        }

                        break;
                    }
                    default:
                    {
                        if (dictionary is IEnumerable<object> t)
                        {
                            foreach (var tt in t)
                            {
                                result.AddRange(Query<T>(tt, key, propertyName, key));
                            }
                        }
                        break;
                    }
                }

                return result;
            }
        }
        
        private static IEnumerable<(string, string)> ReadLeaf(YamlNode node, string currentNodeName)
        {
            const string valueNodeName = "/value/";
            
            var response = new List<(string, string)>();
            if (node is YamlMappingNode)
            {
                foreach (var child in ((YamlMappingNode)node).Children)
                {
                    response.AddRange(ReadLeaf(child.Value, $"{currentNodeName}/{((YamlScalarNode)child.Key).Value}"));
                }
            }
            else if (currentNodeName.Contains(valueNodeName) && currentNodeName.Contains($"/{CurrentEnvironment}/")) // TODO PB2- Change dev to an environment variable
            {
                var path = currentNodeName.Substring(0, currentNodeName.IndexOf(valueNodeName, StringComparison.Ordinal));
                string value;
                
                switch (node)
                {
                    case YamlSequenceNode sequenceNode:
                    {
                        value = sequenceNode.Children.Select(s => (YamlScalarNode) s).Aggregate(
                                new StringBuilder(), 
                                (current, next) => current.Append(current.Length == 0? string.Empty : ",").Append(next))
                            .ToString();

                        Console.WriteLine($"path: {path} ... value: {value}");
                        response.Add((path, value));
                        break;
                    }
                    case YamlScalarNode scalarNodeNode:
                    {
                        value = scalarNodeNode.Value;
                    
                        response.Add((path, value));
                        break;
                    }
                }
            }
            
            return response;
        }
    }
}