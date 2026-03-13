/*
┌────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                       │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-Package-Template) │
│  Copyright (c) 2025 Ivan Murzak                                            │
│  Licensed under the MIT License.                                           │
│  See the LICENSE file in the project root for more information.            │
└────────────────────────────────────────────────────────────────────────────┘
*/
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.ProBuilder.Installer.Tests
{
    public class OpenUpmVersionTests
    {
        const string SampleResponse = @"{
            ""name"": ""com.example.package"",
            ""versions"": {
                ""1.0.0"": { ""version"": ""1.0.0"" },
                ""1.0.10"": { ""version"": ""1.0.10"" },
                ""1.0.20"": { ""version"": ""1.0.20"" },
                ""1.0.30"": { ""version"": ""1.0.30"" },
                ""1.0.40"": { ""version"": ""1.0.40"" },
                ""1.0.42"": { ""version"": ""1.0.42"" }
            }
        }";

        [Test]
        public void ParseBestVersion_ExactVersionExists_ReturnsIt()
        {
            var result = Installer.ParseBestVersion(SampleResponse, "1.0.42");

            Assert.AreEqual("1.0.42", result,
                "Should return the exact version when it exists on OpenUPM");
        }

        [Test]
        public void ParseBestVersion_ExactVersionMissing_ReturnsHighestBelow()
        {
            var result = Installer.ParseBestVersion(SampleResponse, "1.0.45");

            Assert.AreEqual("1.0.42", result,
                "Should return the highest available version below the target");
        }

        [Test]
        public void ParseBestVersion_OnlyNewerVersionsExist_ReturnsNull()
        {
            var result = Installer.ParseBestVersion(SampleResponse, "0.0.1");

            Assert.IsNull(result,
                "Should return null when all available versions are newer than the target");
        }

        [Test]
        public void ParseBestVersion_MalformedJson_ReturnsNull()
        {
            var result = Installer.ParseBestVersion("not valid json {{{", "1.0.42");

            Assert.IsNull(result,
                "Should return null for malformed JSON");
        }

        [Test]
        public void ParseBestVersion_EmptyVersions_ReturnsNull()
        {
            var json = @"{ ""name"": ""pkg"", ""versions"": {} }";
            var result = Installer.ParseBestVersion(json, "1.0.42");

            Assert.IsNull(result,
                "Should return null when versions object is empty");
        }

        [Test]
        public void ParseBestVersion_NullInput_ReturnsNull()
        {
            Assert.IsNull(Installer.ParseBestVersion(null, "1.0.42"),
                "Should return null for null JSON input");

            Assert.IsNull(Installer.ParseBestVersion(SampleResponse, null),
                "Should return null for null maxVersion");
        }

        [Test]
        public void ParseBestVersion_EmptyStringInput_ReturnsNull()
        {
            Assert.IsNull(Installer.ParseBestVersion("", "1.0.42"),
                "Should return null for empty JSON input");

            Assert.IsNull(Installer.ParseBestVersion(SampleResponse, ""),
                "Should return null for empty maxVersion");
        }

        [Test]
        public void ParseBestVersion_NoVersionsKey_ReturnsNull()
        {
            var json = @"{ ""name"": ""pkg"" }";
            var result = Installer.ParseBestVersion(json, "1.0.42");

            Assert.IsNull(result,
                "Should return null when JSON has no versions key");
        }

        [Test]
        public void ParseBestVersion_SkipsInvalidVersionStrings()
        {
            var json = @"{
                ""versions"": {
                    ""invalid"": {},
                    ""1.0.10"": {},
                    ""also-bad"": {},
                    ""1.0.0"": {}
                }
            }";
            var result = Installer.ParseBestVersion(json, "1.0.42");

            Assert.AreEqual("1.0.10", result,
                "Should skip invalid version strings and return the highest valid one");
        }

        [Test]
        public void ParseBestVersion_SelectsCorrectVersionAmongMany()
        {
            var result = Installer.ParseBestVersion(SampleResponse, "1.0.40");

            Assert.AreEqual("1.0.40", result,
                "Should select the exact version when it exists, even when higher versions are available");
        }
    }
}
