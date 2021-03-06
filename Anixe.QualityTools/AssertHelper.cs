using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using FluentAssertions;
using FluentAssertions.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Xunit;

namespace Anixe.QualityTools
{
    public static class AssertHelper
    {
        public static void AreXmlDocumentsEqual(string expected, string actual)
        {
            var actualXml = XDocument.Parse(actual);
            var expectedXml = XDocument.Parse(expected);
            try
            {
                actualXml.Should().BeEquivalentTo(expectedXml);
            }
            catch (Xunit.Sdk.XunitException ex)
            {
                var sb = new StringBuilder()
                  .AppendLine(ex.Message)
                  .AppendLine()
                  .AppendLine("################### expected:")
                  .AppendLine(expectedXml.ToString())
                  .AppendLine()
                  .AppendLine("################### actual:")
                  .AppendLine(actualXml.ToString());

                throw new Xunit.Sdk.XunitException(sb.ToString());
            }
        }

        public static void AreJsonObjectsEqual(string expected, string actual)
        {
            var actualObject = JToken.Parse(actual);
            var expectedObject = JToken.Parse(expected);
            actualObject.Should().BeEquivalentTo(expectedObject);
        }

        public static void AreJsonObjectsSemanticallyEqual(string expected, string actual)
        {
            var expectedObject = JsonConvert.DeserializeObject<JToken>(expected);
            var actualObject = JsonConvert.DeserializeObject<JToken>(actual);

            try
            {
                SemanticallyEqual(expectedObject, actualObject);
            }
            catch (Xunit.Sdk.XunitException ex)
            {
                var sb = new StringBuilder()
                  .AppendLine("################### Expected:")
                  .AppendLine(JsonConvert.SerializeObject(expectedObject, Newtonsoft.Json.Formatting.Indented))
                  .AppendLine()
                  .AppendLine("******************* Actual:")
                  .AppendLine(JsonConvert.SerializeObject(actualObject, Newtonsoft.Json.Formatting.Indented))
                  .AppendLine()
                  .AppendLine(ex.Message);

                throw new Xunit.Sdk.XunitException(sb.ToString());
            }
        }

        public static void AssertCollection<T>(List<T> expected, List<T> actual, Action<T, T> assertItem)
        {
            if (expected == null)
            {
                Assert.Null(actual);
            }
            else
            {
                Assert.NotNull(actual);
                Assert.Equal(expected.Count, actual.Count);
                for (int i = 0; i < expected.Count; i++)
                {
                    assertItem(expected[i], actual[i]);
                }
            }
        }

        private static void SemanticallyEqual(JToken left, JToken right)
        {
            if (left.Type != right.Type)
            {
                throw new Xunit.Sdk.XunitException($"Token of path '{left.Path}' of type {left.Type} is different from '{right.Path}' of type {right.Type}");
            }

            switch (left.Type)
            {
                case(JTokenType.Object):
                    var leftObject = (left as JObject);
                    var rightObject = (right as JObject);

                    if (leftObject.Count != rightObject.Count)
                    {
                        throw new Xunit.Sdk.XunitException($"Objects for path {left.Path} are different.{System.Environment.NewLine}Expected:{System.Environment.NewLine}{leftObject.ToString()}{System.Environment.NewLine}Actual:{System.Environment.NewLine}{rightObject.ToString()}");
                    }

                    foreach (var leftObjectItem in leftObject)
                    {
                        JToken rightObjectItem;

                        if (rightObject.TryGetValue(leftObjectItem.Key, out rightObjectItem))
                        {
                            SemanticallyEqual(leftObjectItem.Value, rightObjectItem);
                        }
                        else
                        {
                            throw new Xunit.Sdk.XunitException($"Property: '{leftObject.Path}.{leftObjectItem.Key}' is missing in actual object{System.Environment.NewLine}Expected:{System.Environment.NewLine}{leftObject.ToString()}{System.Environment.NewLine}Actual:{System.Environment.NewLine}{rightObject.ToString()}");
                        }
                    }

                    break;
                case(JTokenType.Array):
                    var leftChildren = (left as JArray);
                    var rightChildren = (right as JArray);

                    if (leftChildren.Count != rightChildren.Count)
                    {
                        throw new Xunit.Sdk.XunitException($"Arrays for path {left.Path} have different length.{System.Environment.NewLine}Expected {left.ToString()}{System.Environment.NewLine}Actual:{System.Environment.NewLine}{right.ToString()}");
                    }

                    for (int i = 0; i < leftChildren.Count; i++)
                    {
                        SemanticallyEqual(leftChildren[i], rightChildren[i]);
                    }

                    break;

                default: // non composite data structure found
                    if (!JToken.DeepEquals(left, right))
                    {
                        throw new Xunit.Sdk.XunitException($"Values for path {left.Path} are different.{System.Environment.NewLine}Expected: {left.ToString()}{System.Environment.NewLine}Actual: {right.ToString()}");
                    }

                    break;
            }
        }
    }
}