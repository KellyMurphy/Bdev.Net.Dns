using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Bdev.Net.Dns.Helpers;
using NUnit.Framework;

namespace Bdev.Net.Dns.NUnit
{
    /// <summary>
    ///     Summary description for CorrectBehaviour.
    /// </summary>
    [TestFixture]
    public class CorrectBehaviour
    {
        [TestCase("mercedes-benz.com")]
        [TestCase("Google.com")]
        [TestCase("ibm.com")]
        public void DomainsMustPass(string name)
        {
            var res = DnsServers.Resolve(name);
            Assert.IsNotNullOrEmpty(res.First().IPAddress.ToString());
            Trace.WriteLine(string.Join(Environment.NewLine, res.Select(s => s.IPAddress)));
        }

        [Test]
        public void CompareTwoRecords()
        {
            var request = new Request();
            request.AddQuestion(new Question("google.com", DnsType.ANAME));

            var first = Resolver.Lookup(request, IPAddress.Parse("1.1.1.1")).Answers.OrderBy(o => o.Record).First();
            var second = Resolver.Lookup(request, IPAddress.Parse("1.0.0.1")).Answers.OrderBy(o => o.Record).First();
            Assert.True(first.Equals(second));
        }

        [Test]
        public void CompareTwoRecordsOneDefault()
        {
            var request = new Request {RecursionDesired = true};
            var value = "google.com";
            request.AddQuestion(new Question(value, DnsType.ANAME));

            var firstAnswers = Resolver.Lookup(value).Answers;
            var first = firstAnswers.OrderBy(o => o.Record).First();
            var secondAnswers = Resolver.Lookup(request, IPAddress.Parse("192.168.3.1")).Answers;
            var second = secondAnswers.OrderBy(o => o.Record).First();
            Assert.True(first.Equals(second));
        }

        [Test]
        public void TextRecordsMustExist()
        {
            var request = new Request {RecursionDesired = true}.WithQuestion(new Question("quaterne.com", DnsType.TXT));
            var result = Resolver.Lookup(request);
            var l= result.Answers.Select(s => s.Record).OfType<TXTRecord>().ToList();
            var list = result.Answers.Select(s => s.Record).OfType<TXTRecord>().Select(s => s.Value).ToList();
            Assert.True(result.Answers.Any());
        }

        [Test]
        public void CorrectANameForCodeProject()
        {
            var response = DnsServers.Resolve("codeproject.com").ToList();

            // check the reponse
            Assert.AreEqual(1, response.Count);
            Assert.AreEqual(IPAddress.Parse("76.74.234.210"), response.First().IPAddress);
        }

        [Test]
        public void CorrectMXForCodeProject()
        {
            // also 194.72.0.114
            var records = Resolver.MXLookup("codeproject.com", DnsServers.IP4.First());

            Assert.IsNotNull(records, "MXLookup returning null denoting lookup failure");
            Assert.IsTrue(records.Length > 0);
        }

        [Test]
        public void CorrectMXForCodeProjectCompare()
        {
            var result = DnsServers.Resolve<MXRecord>("codeproject.com", DnsType.MX, DnsClass.IN);

            var records = Resolver.MXLookup("codeproject.com", DnsServers.IP4.First());
            Assert.IsNotNull(records, "MXLookup returning null denoting lookup failure");
            Assert.IsTrue(records.Length > 0);

            Assert.IsTrue(records.All(a => result.Contains(a)));
            Assert.IsTrue(result.All(a => records.Contains(a)));
        }

        [Test]
        public void CorrectNSForCodeProject()
        {
            // also 194.72.0.114
            // create a new request
            var request = new Request();

            // add the codeproject ANAME question
            request.AddQuestion(new Question("codeproject.com", DnsType.NS));

            // send the request
            var response = Resolver.Lookup(request, DnsServers.IP4.First());

            // check the reponse
            Assert.AreEqual(ReturnCode.Success, response.ReturnCode);

            // we expect 4 records
            Assert.IsTrue(response.Answers.Length > 0);
        }

        [Test]
        [ExpectedException(typeof(NoResponseException))]
        public void NoResponseForBadDnsAddress()
        {
            Resolver.MXLookup("codeproject.com", IPAddress.Parse("127.0.0.1"));
        }

        [Test]
        public void RepeatedANameLookups()
        {
            Parallel.For(0, 100, i =>
            {
                // send the request
                var response = DnsServers.Resolve("codeproject.com").ToArray();

                // check the reponse
                Assert.AreEqual(1, response.Length);
            });
        }
    }
}