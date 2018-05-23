// Copyright © 2014 Rick Beerendonk. All Rights Reserved.
//
// This code is a C# port of the Java version created and maintained by Cognitect, therefore
//
// Copyright © 2014 Cognitect. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS-IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Moq;
using Beerendonk.Transit.Impl;
using Beerendonk.Transit.Java;
using Beerendonk.Transit.Numerics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using Xunit;

namespace Beerendonk.Transit.Tests
{
    public class TransitTest
    {
        #region Reading

        public IReader Reader(string s)
        {
            Stream input = new MemoryStream(Encoding.Default.GetBytes(s));
            return TransitFactory.Reader(TransitFactory.Format.Json, input);
        }

        [Fact]
        public void TestReadString()
        {
            Assert.Equal("foo", Reader("\"foo\"").Read<string>());
            Assert.Equal("~foo", Reader("\"~~foo\"").Read<string>());
            Assert.Equal("`foo", Reader("\"~`foo\"").Read<string>());
            Assert.Equal("foo", Reader("\"~#foo\"").Read<Tag>().GetValue());
            Assert.Equal("^foo", Reader("\"~^foo\"").Read<string>());
        }

        [Fact]
        public void TestReadBoolean()
        {
            Assert.True(Reader("\"~?t\"").Read<bool>());
            Assert.False(Reader("\"~?f\"").Read<bool>());

            IDictionary d = Reader("{\"~?t\":1,\"~?f\":2}").Read<IDictionary>();
            Assert.Equal(1L, d[true]);
            Assert.Equal(2L, d[false]);
        }

        [Fact]
        public void TestReadNull()
        {
            IReader r = Reader("\"~_\"");
            object v = r.Read<object>();
            Assert.Null(v);
        }

        [Fact]
        public void TestReadKeyword()
        {
            IKeyword v = Reader("\"~:foo\"").Read<IKeyword>();
            Assert.Equal("foo", v.ToString());

            IReader r = Reader("[\"~:foo\",\"^" + (char)WriteCache.BaseCharIdx + "\",\"^" + (char)WriteCache.BaseCharIdx + "\"]");
            IList v2 = r.Read<IList>();
            Assert.Equal("foo", v2[0].ToString());
            Assert.Equal("foo", v2[1].ToString());
            Assert.Equal("foo", v2[2].ToString());
        }

        [Fact]
        public void TestReadInteger()
        {
            IReader r = Reader("\"~i42\"");
            long v = r.Read<long>();
            Assert.Equal<long>(42L, v);
        }

        [Fact]
        public void TestReadBigInteger()
        {
            BigInteger expected = BigInteger.Parse("4256768765123454321897654321234567");
            IReader r = Reader("\"~n4256768765123454321897654321234567\"");
            BigInteger v = r.Read<BigInteger>();
            Assert.Equal<BigInteger>(expected, v);
        }

        [Fact]
        public void TestReadDouble()
        {
            Assert.Equal<double>(42.5D, Reader("\"~d42.5\"").Read<double>());
        }

        [Fact]
        public void TestReadSpecialNumbers()
        {
            Assert.Equal<double>(double.NaN, Reader("\"~zNaN\"").Read<double>());
            Assert.Equal<double>(double.PositiveInfinity, Reader("\"~zINF\"").Read<double>());
            Assert.Equal<double>(double.NegativeInfinity, Reader("\"~z-INF\"").Read<double>());
        }

        [Fact]
        public void TestReadBigRational()
        {
            Assert.Equal(new BigRational(12.345M), Reader("\"~f12.345\"").Read<BigRational>());
            Assert.Equal(new BigRational(-12.345M), Reader("\"~f-12.345\"").Read<BigRational>());
            Assert.Equal(new BigRational(0.1001M), Reader("\"~f0.1001\"").Read<BigRational>());
            Assert.Equal(new BigRational(0.01M), Reader("\"~f0.01\"").Read<BigRational>());
            Assert.Equal(new BigRational(0.1M), Reader("\"~f0.1\"").Read<BigRational>());
            Assert.Equal(new BigRational(1M), Reader("\"~f1\"").Read<BigRational>());
            Assert.Equal(new BigRational(10M), Reader("\"~f10\"").Read<BigRational>());
            Assert.Equal(new BigRational(420.0057M), Reader("\"~f420.0057\"").Read<BigRational>());
        }

        [Fact]
        public void TestReadDateTime()
        {
            var d = new DateTime(2014, 8, 9, 10, 6, 21, 497, DateTimeKind.Local);
            var expected = new DateTimeOffset(d).LocalDateTime;
            long javaTime = Beerendonk.Transit.Java.Convert.ToJavaTime(d);

            string timeString = JsonParser.FormatDateTime(d);
            Assert.Equal(expected, Reader("\"~t" + timeString + "\"").Read<DateTime>());

            Assert.Equal(expected, Reader("{\"~#m\": " + javaTime + "}").Read<DateTime>());

            timeString = new DateTimeOffset(d).UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'");
            Assert.Equal(expected, Reader("\"~t" + timeString + "\"").Read<DateTime>());

            timeString = new DateTimeOffset(d).UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
            Assert.Equal(expected.AddMilliseconds(-497D), Reader("\"~t" + timeString + "\"").Read<DateTime>());

            timeString = new DateTimeOffset(d).UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff-00:00");
            Assert.Equal(expected, Reader("\"~t" + timeString + "\"").Read<DateTime>());
        }

        [Fact]
        public void TestReadGuid()
        {
            Guid guid = Guid.NewGuid();
            long hi64 = ((Uuid)guid).MostSignificantBits;
            long lo64 = ((Uuid)guid).LeastSignificantBits;

            Assert.Equal(guid, Reader("\"~u" + guid.ToString() + "\"").Read<Guid>());
            Assert.Equal(guid, Reader("{\"~#u\": [" + hi64 + ", " + lo64 + "]}").Read<Guid>());
        }

        [Fact]
        public void TestReadUri()
        {
            Uri expected = new Uri("http://www.foo.com");
            IReader r = Reader("\"~rhttp://www.foo.com\"");
            Uri v = r.Read<Uri>();
            Assert.Equal(expected, v);
        }

        [Fact]
        public void TestReadSymbol()
        {
            IReader r = Reader("\"~$foo\"");
            ISymbol v = r.Read<ISymbol>();
            Assert.Equal("foo", v.ToString());
        }

        [Fact]
        public void TestReadCharacter()
        {
            IReader r = Reader("\"~cf\"");
            char v = r.Read<char>();
            Assert.Equal('f', v);
        }

        [Fact]
        public void TestReadBinary()
        {
            byte[] bytes = Encoding.ASCII.GetBytes("foobarbaz");
            string encoded = System.Convert.ToBase64String(bytes);
            byte[] decoded = Reader("\"~b" + encoded + "\"").Read<byte[]>();

            Assert.Equal(bytes.Length, decoded.Length);

            bool same = true;
            for (int i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] != decoded[i])
                    same = false;
            }

            Assert.True(same);
        }

        [Fact]
        public void TestReadUnknown()
        {
            Assert.Equal(TransitFactory.TaggedValue("j", "foo"), Reader("\"~jfoo\"").Read<ITaggedValue>());
            IList<object> l = new List<object> { 1L, 2L };

            ITaggedValue expected = TransitFactory.TaggedValue("point", l);
            ITaggedValue result = Reader("{\"~#point\":[1,2]}").Read<ITaggedValue>();
            Assert.Equal(expected.Tag, result.Tag);
            Assert.Equal(((IList<object>)expected.Representation).ToArray(), ((IList<object>)result.Representation).ToArray());
        }

        [Fact]
        public void TestReadList()
        {
            IList l = Reader("[1, 2, 3]").Read<IList>();

            Assert.True(l is IList<object>);
            Assert.Equal(3, l.Count);

            Assert.Equal(1L, l[0]);
            Assert.Equal(2L, l[1]);
            Assert.Equal(3L, l[2]);
        }

        [Fact]
        public void TestReadListWithNested()
        {
            var d = new DateTime(2014, 8, 10, 13, 34, 35);
            String t = JsonParser.FormatDateTime(d);

            IList l = Reader("[\"~:foo\", \"~t" + t + "\", \"~?t\"]").Read<IList>();

            Assert.Equal(3, l.Count);

            Assert.Equal("foo", l[0].ToString());
            Assert.Equal(d, (DateTime)l[1]);
            Assert.True((bool)l[2]);
        }

        [Fact]
        public void TestReadDictionary()
        {
            IDictionary m = Reader("{\"a\": 2, \"b\": 4}").Read<IDictionary>();

            Assert.Equal(2, m.Count);

            Assert.Equal(2L, m["a"]);
            Assert.Equal(4L, m["b"]);
        }

        [Fact]
        public void TestReadDictionaryWithNested()
        {
            Guid guid = Guid.NewGuid();

            IDictionary m = Reader("{\"a\": \"~:foo\", \"b\": \"~u" + (Uuid)guid + "\"}").Read<IDictionary>();

            Assert.Equal(2, m.Count);

            Assert.Equal("foo", m["a"].ToString());
            Assert.Equal(guid, m["b"]);
        }

        [Fact]
        public void TestReadSet()
        {
            ISet<object> s = Reader("{\"~#set\": [1, 2, 3]}").Read<ISet<object>>();

            Assert.Equal(3, s.Count);

            Assert.True(s.Contains(1L));
            Assert.True(s.Contains(2L));
            Assert.True(s.Contains(3L));
        }

        [Fact]
        public void TestReadEnumerable()
        {
            IEnumerable l = Reader("{\"~#list\": [1, 2, 3]}").Read<IEnumerable>();
            IEnumerable<object> lo = l.OfType<object>();

            Assert.True(l is IEnumerable);
            Assert.Equal(3, lo.Count());

            Assert.Equal(1L, lo.First());
            Assert.Equal(2L, lo.Skip(1).First());
            Assert.Equal(3L, lo.Skip(2).First());
        }

        [Fact]
        public void TestReadRatio()
        {
            IRatio r = Reader("{\"~#ratio\": [\"~n1\",\"~n2\"]}").Read<IRatio>();

            Assert.Equal(BigInteger.One, r.Numerator);
            Assert.Equal(BigInteger.One + 1, r.Denominator);
            Assert.Equal(0.5d, r.GetValue());
        }

        [Fact]
        public void TestReadCDictionary()
        {
            IDictionary m = Reader("{\"~#cmap\": [{\"~#ratio\":[\"~n1\",\"~n2\"]},1,{\"~#list\":[1,2,3]},2]}").Read<IDictionary>();

            Assert.Equal(2, m.Count);

            foreach (DictionaryEntry e in m)
            {
                if ((long)e.Value == 1L)
                {
                    Ratio r = (Ratio)e.Key;
                    Assert.Equal(new BigInteger(1), r.Numerator);
                    Assert.Equal(new BigInteger(2), r.Denominator);
                }
                else
                {
                    if ((long)e.Value == 2L)
                    {
                        IList l = (IList)e.Key;
                        Assert.Equal(1L, l[0]);
                        Assert.Equal(2L, l[1]);
                        Assert.Equal(3L, l[2]);
                    }
                }
            }
        }

        [Fact]
        public void TestReadSetTagAsString()
        {
            object o = Reader("{\"~~#set\": [1, 2, 3]}").Read<object>();
            Assert.False(o is ISet<object>);
            Assert.True(o is IDictionary);
        }

        [Fact]
        public void TestReadMany()
        {
            IReader r;

            // TODO Make sure JSON parser can parse number larger than Int64
            /*
            BigInteger expected = BigInteger.Parse("4256768765123454321897654321234567");
            r = Reader("4256768765123454321897654321234567");
            BigInteger v = r.Read<BigInteger>();
            Assert.Equal<BigInteger>(expected, v);
            */

            r = Reader("true null false \"foo\" 42.2 42");
            Assert.True(Reader("true").Read<bool>());
            Assert.Null(Reader("null").Read<object>());
            Assert.False(Reader("false").Read<bool>());
            Assert.Equal("foo", Reader("\"foo\"").Read<string>());
            Assert.Equal(42.2, Reader("42.2").Read<double>());
            Assert.Equal(42L, Reader("42").Read<long>());
        }

        [Fact]
        public void TestReadCache()
        {
            ReadCache rc = new ReadCache();
            Assert.Equal("~:foo", rc.CacheRead("~:foo", false));
            Assert.Equal("~:foo", rc.CacheRead("^" + (char)WriteCache.BaseCharIdx, false));
            Assert.Equal("~$bar", rc.CacheRead("~$bar", false));
            Assert.Equal("~$bar", rc.CacheRead("^" + (char)(WriteCache.BaseCharIdx + 1), false));
            Assert.Equal("~#baz", rc.CacheRead("~#baz", false));
            Assert.Equal("~#baz", rc.CacheRead("^" + (char)(WriteCache.BaseCharIdx + 2), false));
            Assert.Equal("foobar", rc.CacheRead("foobar", false));
            Assert.Equal("foobar", rc.CacheRead("foobar", false));
            Assert.Equal("foobar", rc.CacheRead("foobar", true));
            Assert.Equal("foobar", rc.CacheRead("^" + (char)(WriteCache.BaseCharIdx + 3), true));
            Assert.Equal("abc", rc.CacheRead("abc", false));
            Assert.Equal("abc", rc.CacheRead("abc", false));
            Assert.Equal("abc", rc.CacheRead("abc", true));
            Assert.Equal("abc", rc.CacheRead("abc", true));
        }

        [Fact]
        public void TestReadIdentity()
        {
            IReader r = Reader("\"~\\'42\"");
            string v = r.Read<string>();
            Assert.Equal<string>("42", v);
        }

        [Fact]
        public void TestReadLink()
        {
            IReader r = Reader("[\"~#link\" , {\"href\": \"~rhttp://www.Beerendonk.nl\", \"rel\": \"a-rel\", \"name\": \"a-name\", \"prompt\": \"a-prompt\", \"render\": \"link or image\"}]");
            ILink v = r.Read<ILink>();
            Assert.Equal(new Uri("http://www.Beerendonk.nl"), v.Href);
            Assert.Equal("a-rel", v.Rel);
            Assert.Equal("a-name", v.Name);
            Assert.Equal("a-prompt", v.Prompt);
            Assert.Equal("link or image", v.Render);
        }

        #endregion

        #region Writing

        public string Write(object obj, TransitFactory.Format format, IDictionary<Type, IWriteHandler> customHandlers)
        {
            using (Stream output = new MemoryStream())
            {
                IWriter<object> w = TransitFactory.Writer<object>(format, output, customHandlers);
                w.Write(obj);

                output.Position = 0;
                var sr = new StreamReader(output);
                return sr.ReadToEnd();
            }
        }

        public string WriteJsonVerbose(object obj)
        {
            return Write(obj, TransitFactory.Format.JsonVerbose, null);
        }

        public string WriteJsonVerbose(object obj, IDictionary<Type, IWriteHandler> customHandlers)
        {
            return Write(obj, TransitFactory.Format.JsonVerbose, customHandlers);
        }

        public string WriteJson(object obj)
        {
            return Write(obj, TransitFactory.Format.Json, null);
        }

        public string WriteJson(object obj, IDictionary<Type, IWriteHandler> customHandlers)
        {
            return Write(obj, TransitFactory.Format.Json, customHandlers);
        }

        public bool IsEqual(object o1, object o2)
        {
            if (o1 is bool && o2 is bool)
                return (bool)o1 == (bool)o2;
            else
                return false;
        }

        [Fact]
        public void TestRoundTrip()
        {
            object inObject = true;
            object outObject;

            string s;

            using (Stream output = new MemoryStream())
            {
                IWriter<object> w = TransitFactory.Writer<object>(TransitFactory.Format.JsonVerbose, output);
                w.Write(inObject);

                output.Position = 0;
                var sr = new StreamReader(output);
                s = sr.ReadToEnd();
            }

            byte[] buffer = Encoding.ASCII.GetBytes(s);
            using (Stream input = new MemoryStream(buffer))
            {
                IReader reader = TransitFactory.Reader(TransitFactory.Format.Json, input);
                outObject = reader.Read<object>();
            }

            Assert.True(IsEqual(inObject, outObject));
        }

        public string Scalar(string value)
        {
            return "[\"~#'\"," + value + "]";
        }

        public string ScalarVerbose(string value)
        {
            return "{\"~#'\":" + value + "}";
        }

        [Fact]
        public void TestWriteNull()
        {
            Assert.Equal(ScalarVerbose("null"), WriteJsonVerbose(null));
            Assert.Equal(Scalar("null"), WriteJson(null));
        }

        [Fact]
        public void TestWriteKeyword()
        {
            Assert.Equal(ScalarVerbose("\"~:foo\""), WriteJsonVerbose(TransitFactory.Keyword("foo")));
            Assert.Equal(Scalar("\"~:foo\""), WriteJson(TransitFactory.Keyword("foo")));

            IList l = new IKeyword[] 
            {
                TransitFactory.Keyword("foo"),
                TransitFactory.Keyword("foo"),
                TransitFactory.Keyword("foo")
            };
            Assert.Equal("[\"~:foo\",\"~:foo\",\"~:foo\"]", WriteJsonVerbose(l));
            Assert.Equal("[\"~:foo\",\"^0\",\"^0\"]", WriteJson(l));
        }

        [Fact]
        public void TestWriteObjectJson()
        {
            Assert.Throws<NotSupportedException>(() => WriteJson(new object()));
        }

        [Fact]
        public void TestWriteObjectJsonVerbose()
        {
            Assert.Throws<NotSupportedException>(() => WriteJsonVerbose(new object()));
        }

        [Fact]
        public void TestWriteString()
        {
            Assert.Equal(ScalarVerbose("\"foo\""), WriteJsonVerbose("foo"));
            Assert.Equal(Scalar("\"foo\""), WriteJson("foo"));
            Assert.Equal(ScalarVerbose("\"~~foo\""), WriteJsonVerbose("~foo"));
            Assert.Equal(Scalar("\"~~foo\""), WriteJson("~foo"));
        }

        [Fact]
        public void TestWriteBoolean()
        {
            Assert.Equal(ScalarVerbose("true"), WriteJsonVerbose(true));
            Assert.Equal(Scalar("true"), WriteJson(true));
            Assert.Equal(Scalar("false"), WriteJson(false));

            var d = new Dictionary<bool, int>();
            d[true] = 1;
            Assert.Equal("{\"~?t\":1}", WriteJsonVerbose(d));
            Assert.Equal("[\"^ \",\"~?t\",1]", WriteJson(d));

            var d2 = new Dictionary<bool, int>();
            d2[false] = 1;
            Assert.Equal("{\"~?f\":1}", WriteJsonVerbose(d2));
            Assert.Equal("[\"^ \",\"~?f\",1]", WriteJson(d2));
        }

        [Fact]
        public void TestWriteInteger()
        {
            Assert.Equal(ScalarVerbose("42"), WriteJsonVerbose(42));
            Assert.Equal(ScalarVerbose("42"), WriteJsonVerbose(42L));
            Assert.Equal(ScalarVerbose("42"), WriteJsonVerbose((byte)42));
            Assert.Equal(ScalarVerbose("42"), WriteJsonVerbose((short)42));
            Assert.Equal(ScalarVerbose("42"), WriteJsonVerbose((int)42));
            Assert.Equal(ScalarVerbose("42"), WriteJsonVerbose(42L));
            Assert.Equal(ScalarVerbose("\"~n42\""), WriteJsonVerbose(BigInteger.Parse("42")));
            Assert.Equal(ScalarVerbose("\"~n4256768765123454321897654321234567\""), WriteJsonVerbose(BigInteger.Parse("4256768765123454321897654321234567")));
        }

        [Fact]
        public void TestWriteFloatDouble()
        {
            Assert.Equal(ScalarVerbose("42.5"), WriteJsonVerbose(42.5));
            Assert.Equal(ScalarVerbose("42.5"), WriteJsonVerbose(42.5F));
            Assert.Equal(ScalarVerbose("42.5"), WriteJsonVerbose(42.5D));
        }

        [Fact]
        public void TestSpecialNumbers()
        {
            Assert.Equal(Scalar("\"~zNaN\""), WriteJson(double.NaN));
            Assert.Equal(Scalar("\"~zINF\""), WriteJson(double.PositiveInfinity));
            Assert.Equal(Scalar("\"~z-INF\""), WriteJson(double.NegativeInfinity));

            Assert.Equal(Scalar("\"~zNaN\""), WriteJson(float.NaN));
            Assert.Equal(Scalar("\"~zINF\""), WriteJson(float.PositiveInfinity));
            Assert.Equal(Scalar("\"~z-INF\""), WriteJson(float.NegativeInfinity));

            Assert.Equal(ScalarVerbose("\"~zNaN\""), WriteJsonVerbose(double.NaN));
            Assert.Equal(ScalarVerbose("\"~zINF\""), WriteJsonVerbose(double.PositiveInfinity));
            Assert.Equal(ScalarVerbose("\"~z-INF\""), WriteJsonVerbose(double.NegativeInfinity));

            Assert.Equal(ScalarVerbose("\"~zNaN\""), WriteJsonVerbose(float.NaN));
            Assert.Equal(ScalarVerbose("\"~zINF\""), WriteJsonVerbose(float.PositiveInfinity));
            Assert.Equal(ScalarVerbose("\"~z-INF\""), WriteJsonVerbose(float.NegativeInfinity));
        }

        [Fact]
        public void TestWriteBigDecimal()
        {
            // TODO
            //Assert.Equal(ScalarVerbose("\"~f42.5\""), WriteJsonVerbose(new BigRational(42.5)));
        }

        [Fact]
        public void TestWriteDateTime()
        {
            var d = DateTime.Now;
            String dateString = AbstractParser.FormatDateTime(d);
            long dateLong = Beerendonk.Transit.Java.Convert.ToJavaTime(d);
            Assert.Equal(ScalarVerbose("\"~t" + dateString + "\""), WriteJsonVerbose(d));
            Assert.Equal(Scalar("\"~m" + dateLong + "\""), WriteJson(d));
        }

        [Fact]
        public void TestWriteUUID()
        {
            Guid guid = Guid.NewGuid();
            Assert.Equal(ScalarVerbose("\"~u" + guid.ToString() + "\""), WriteJsonVerbose(guid));
        }

        [Fact]
        public void TestWriteURI()
        {
            Uri uri = new Uri("http://www.foo.com/");

            Assert.Equal(ScalarVerbose("\"~rhttp://www.foo.com/\""), WriteJsonVerbose(uri));
        }

        [Fact]
        public void TestWriteBinary()
        {
            byte[] bytes = Encoding.ASCII.GetBytes("foobarbaz");
            string encoded = System.Convert.ToBase64String(bytes);

            Assert.Equal(ScalarVerbose("\"~b" + encoded + "\""), WriteJsonVerbose(bytes));
        }

        [Fact]
        public void TestWriteSymbol()
        {
            Assert.Equal(ScalarVerbose("\"~$foo\""), WriteJsonVerbose(TransitFactory.Symbol("foo")));
        }

        [Fact]
        public void TestWriteList()
        {
            IList<int> l = new List<int> { 1, 2, 3 };

            Assert.Equal("[1,2,3]", WriteJsonVerbose(l));
            Assert.Equal("[1,2,3]", WriteJson(l));
        }

        [Fact]
        public void TestWritePrimitiveArrays()
        {
            int[] ints = { 1, 2 };
            Assert.Equal("[1,2]", WriteJsonVerbose(ints));

            long[] longs = { 1L, 2L };
            Assert.Equal("[1,2]", WriteJsonVerbose(longs));

            float[] floats = { 1.5f, 2.78f };
            Assert.Equal("[1.5,2.78]", WriteJsonVerbose(floats));

            bool[] bools = { true, false };
            Assert.Equal("[true,false]", WriteJsonVerbose(bools));

            double[] doubles = { 1.654d, 2.8765d };
            Assert.Equal("[1.654,2.8765]", WriteJsonVerbose(doubles));

            short[] shorts = { 1, 2 };
            Assert.Equal("[1,2]", WriteJsonVerbose(shorts));

            char[] chars = { '5', '/' };
            Assert.Equal("[\"~c5\",\"~c/\"]", WriteJsonVerbose(chars));
        }

        [Fact]
        public void TestWriteDictionary()
        {
            IDictionary<string, int> d = new Dictionary<string, int> { {"foo", 1}, {"bar", 2} };

            Assert.Equal("{\"foo\":1,\"bar\":2}", WriteJsonVerbose(d));
            Assert.Equal("[\"^ \",\"foo\",1,\"bar\",2]", WriteJson(d));
        }

        [Fact]
        public void TestWriteEmptyDictionary()
        {
            IDictionary<object, object> d = new Dictionary<object, object>();
            Assert.Equal("{}", WriteJsonVerbose(d));
            Assert.Equal("[\"^ \"]", WriteJson(d));
        }

        [Fact]
        public void TestWriteSet()
        {
            ISet<string> s = new HashSet<string> { "foo", "bar" };

            Assert.Equal("{\"~#set\":[\"foo\",\"bar\"]}", WriteJsonVerbose(s));
            Assert.Equal("[\"~#set\",[\"foo\",\"bar\"]]", WriteJson(s));
        }

        [Fact]
        public void TestWriteEmptySet()
        {
            ISet<object> s = new HashSet<object>();
            Assert.Equal("{\"~#set\":[]}", WriteJsonVerbose(s));
            Assert.Equal("[\"~#set\",[]]", WriteJson(s));
        }

        [Fact]
        public void TestWriteEnumerable()
        {
            ICollection<string> c = new LinkedList<string>();
            c.Add("foo");
            c.Add("bar");
            IEnumerable<string> e = c;
            Assert.Equal("{\"~#list\":[\"foo\",\"bar\"]}", WriteJsonVerbose(e));
            Assert.Equal("[\"~#list\",[\"foo\",\"bar\"]]", WriteJson(e));
        }

        [Fact]
        public void TestWriteEmptyEnumerable()
        {
            IEnumerable<string> c = new LinkedList<string>();
            Assert.Equal("{\"~#list\":[]}", WriteJsonVerbose(c));
            Assert.Equal("[\"~#list\",[]]", WriteJson(c));
        }

        [Fact]
        public void TestWriteCharacter()
        {
            Assert.Equal(ScalarVerbose("\"~cf\""), WriteJsonVerbose('f'));
        }

        [Fact]
        public void TestWriteRatio()
        {
            IRatio r = new Ratio(BigInteger.One, new BigInteger(2));
            Assert.Equal("{\"~#ratio\":[\"~n1\",\"~n2\"]}", WriteJsonVerbose(r));
            Assert.Equal("[\"~#ratio\",[\"~n1\",\"~n2\"]]", WriteJson(r));
        }

        [Fact]
        public void TestWriteCDictionary()
        {
            IRatio r = new Ratio(BigInteger.One, new BigInteger(2));
            IDictionary<object, object> d = new Dictionary<object, object>();
            d.Add(r, 1);
            Assert.Equal("{\"~#cmap\":[{\"~#ratio\":[\"~n1\",\"~n2\"]},1]}", WriteJsonVerbose(d));
            Assert.Equal("[\"~#cmap\",[[\"~#ratio\",[\"~n1\",\"~n2\"]],1]]", WriteJson(d));
        }

        [Fact]
        public void TestWriteCache()
        {
            WriteCache wc = new WriteCache(true);
            Assert.Equal("~:foo", wc.CacheWrite("~:foo", false));
            Assert.Equal("^" + (char)WriteCache.BaseCharIdx, wc.CacheWrite("~:foo", false));
            Assert.Equal("~$bar", wc.CacheWrite("~$bar", false));
            Assert.Equal("^" + (char)(WriteCache.BaseCharIdx + 1), wc.CacheWrite("~$bar", false));
            Assert.Equal("~#baz", wc.CacheWrite("~#baz", false));
            Assert.Equal("^" + (char)(WriteCache.BaseCharIdx + 2), wc.CacheWrite("~#baz", false));
            Assert.Equal("foobar", wc.CacheWrite("foobar", false));
            Assert.Equal("foobar", wc.CacheWrite("foobar", false));
            Assert.Equal("foobar", wc.CacheWrite("foobar", true));
            Assert.Equal("^" + (char)(WriteCache.BaseCharIdx + 3), wc.CacheWrite("foobar", true));
            Assert.Equal("abc", wc.CacheWrite("abc", false));
            Assert.Equal("abc", wc.CacheWrite("abc", false));
            Assert.Equal("abc", wc.CacheWrite("abc", true));
            Assert.Equal("abc", wc.CacheWrite("abc", true));
        }

        [Fact]
        public void TestWriteCacheDisabled()
        {
            WriteCache wc = new WriteCache(false);
            Assert.Equal("foobar", wc.CacheWrite("foobar", false));
            Assert.Equal("foobar", wc.CacheWrite("foobar", false));
            Assert.Equal("foobar", wc.CacheWrite("foobar", true));
            Assert.Equal("foobar", wc.CacheWrite("foobar", true));
        }

        [Fact]
        public void TestWriteUnknown()
        {
            var l = new List<object>();
            l.Add("`jfoo");
            Assert.Equal("[\"~`jfoo\"]", WriteJsonVerbose(l));
            Assert.Equal(ScalarVerbose("\"~`jfoo\""), WriteJsonVerbose("`jfoo"));

            var l2 = new List<object>();
            l2.Add(1L);
            l2.Add(2L);
            Assert.Equal("{\"~#point\":[1,2]}", WriteJsonVerbose(TransitFactory.TaggedValue("point", l2)));
        }

        [Fact]
        public void TestWriteWithCustomHandler()
        {
            Mock<IWriteHandler> mock = new Mock<IWriteHandler>();
            mock.Setup(m => m.Tag(It.IsAny<object>())).Returns("s");
            mock.Setup(m => m.Representation(It.IsAny<object>())).Returns("NULL");
            mock.Setup(m => m.StringRepresentation(It.IsAny<object>())).Returns<string>(null);
            mock.Setup(m => m.GetVerboseHandler()).Returns<IWriteHandler>(null);

            IDictionary<Type, IWriteHandler> customHandlers = new Dictionary<Type, IWriteHandler>();
            customHandlers.Add(typeof(NullType), mock.Object);

            // JSON-Verbose
            Assert.Equal(ScalarVerbose("\"NULL\""), WriteJsonVerbose(null, customHandlers));
            mock.Verify(m => m.Representation(null));
            mock.Verify(m => m.GetVerboseHandler());

            // JSON
            mock.ResetCalls();
            Assert.Equal(Scalar("\"NULL\""), WriteJson(null, customHandlers));
            mock.Verify(m => m.Representation(null));
        }

        #endregion

        [Fact]
        public void TestUseIKeywordAsDictionaryKey()
        {
            IDictionary<object, object> d = new Dictionary<object, object>();
            d.Add(TransitFactory.Keyword("foo"), 1);
            d.Add("foo", 2);
            d.Add(TransitFactory.Keyword("bar"), 3);
            d.Add("bar", 4);

            Assert.Equal(1, d[TransitFactory.Keyword("foo")]);
            Assert.Equal(2, d["foo"]);
            Assert.Equal(3, d[TransitFactory.Keyword("bar")]);
            Assert.Equal(4, d["bar"]);
        }

        [Fact]
        public void TestUseISymbolAsDictionaryKey()
        {
            IDictionary<object, object> d = new Dictionary<object, object>();
            d.Add(TransitFactory.Symbol("foo"), 1);
            d.Add("foo", 2);
            d.Add(TransitFactory.Symbol("bar"), 3);
            d.Add("bar", 4);

            Assert.Equal(1, d[TransitFactory.Symbol("foo")]);
            Assert.Equal(2, d["foo"]);
            Assert.Equal(3, d[TransitFactory.Symbol("bar")]);
            Assert.Equal(4, d["bar"]);
        }

        [Fact]
        public void TestKeywordEquality()
        {
            string s = "foo";

            IKeyword k1 = TransitFactory.Keyword("foo");
            IKeyword k2 = TransitFactory.Keyword("!foo".Substring(1));
            IKeyword k3 = TransitFactory.Keyword("bar");

            Assert.Equal(k1, k2);
            Assert.Equal(k2, k1);
            Assert.False(k1.Equals(k3));
            Assert.False(k3.Equals(k1));
            Assert.False(s.Equals(k1));
            Assert.False(k1.Equals(s));
        }

        [Fact]
        public void TestKeywordHashCode()
        {
            string s = "foo";

            IKeyword k1 = TransitFactory.Keyword("foo");
            IKeyword k2 = TransitFactory.Keyword("!foo".Substring(1));
            IKeyword k3 = TransitFactory.Keyword("bar");
            ISymbol symbol = TransitFactory.Symbol("bar");

            Assert.Equal(k1.GetHashCode(), k2.GetHashCode());
            Assert.False(k3.GetHashCode() == k1.GetHashCode());
            Assert.False(symbol.GetHashCode() == k1.GetHashCode());
            Assert.False(s.GetHashCode() == k1.GetHashCode());
        }

        [Fact]
        public void TestKeywordComparator()
        {

            List<IKeyword> l = new List<IKeyword> {
                { TransitFactory.Keyword("bbb") },
                { TransitFactory.Keyword("ccc") },
                { TransitFactory.Keyword("abc") },
                { TransitFactory.Keyword("dab") } };

            l.Sort();

            Assert.Equal("abc", l[0].ToString());
            Assert.Equal("bbb", l[1].ToString());
            Assert.Equal("ccc", l[2].ToString());
            Assert.Equal("dab", l[3].ToString());
        }

        [Fact]
        public void TestSymbolEquality()
        {
            string s = "foo";

            ISymbol sym1 = TransitFactory.Symbol("foo");
            ISymbol sym2 = TransitFactory.Symbol("!foo".Substring(1));
            ISymbol sym3 = TransitFactory.Symbol("bar");

            Assert.Equal(sym1, sym2);
            Assert.Equal(sym2, sym1);
            Assert.False(sym1.Equals(sym3));
            Assert.False(sym3.Equals(sym1));
            Assert.False(s.Equals(sym1));
            Assert.False(sym1.Equals(s));
        }

        [Fact]
        public void TestSymbolHashCode()
        {
            string s = "foo";

            ISymbol sym1 = TransitFactory.Symbol("foo");
            ISymbol sym2 = TransitFactory.Symbol("!foo".Substring(1));
            ISymbol sym3 = TransitFactory.Symbol("bar");
            IKeyword keyword = TransitFactory.Keyword("bar");

            Assert.Equal(sym1.GetHashCode(), sym2.GetHashCode());
            Assert.False(sym3.GetHashCode() == sym1.GetHashCode());
            Assert.False(keyword.GetHashCode() == sym1.GetHashCode());
            Assert.False(s.GetHashCode() == sym1.GetHashCode());
        }

        [Fact]
        public void TestSymbolComparator()
        {

            List<ISymbol> l = new List<ISymbol> {
                { TransitFactory.Symbol("bbb") },
                { TransitFactory.Symbol("ccc") },
                { TransitFactory.Symbol("abc") },
                { TransitFactory.Symbol("dab") } };

            l.Sort();

            Assert.Equal("abc", l[0].ToString());
            Assert.Equal("bbb", l[1].ToString());
            Assert.Equal("ccc", l[2].ToString());
            Assert.Equal("dab", l[3].ToString());
        }

        [Fact]
        public void TestDictionaryWithEscapedKey()
        {
            var d1 = new Dictionary<object, object> { { "~Gfoo", 20L } };
            string str = WriteJson(d1);

            IDictionary d2 = Reader(str).Read<IDictionary>();
            Assert.True(d2.Contains("~Gfoo"));
            Assert.True(d2["~Gfoo"].Equals(20L));
        }

        [Fact]
        public void TestLink()
        {
            ILink l1 = TransitFactory.Link("http://google.com/", "search", "name", "link", "prompt");
            String str = WriteJson(l1);
            ILink l2 = Reader(str).Read<ILink>();
            Assert.Equal("http://google.com/", l2.Href.AbsoluteUri);
            Assert.Equal("search", l2.Rel);
            Assert.Equal("name", l2.Name);
            Assert.Equal("link", l2.Render);
            Assert.Equal("prompt", l2.Prompt);
        }

        [Fact]
        public void TestEmptySet()
        {
            string str = WriteJson(new HashSet<object>());
            Assert.IsAssignableFrom<ISet<object>>(Reader(str).Read<ISet<object>>());
        }
    }
}
