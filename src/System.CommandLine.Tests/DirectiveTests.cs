﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace System.CommandLine.Tests
{
    public class DirectiveTests
    {
        [Fact]
        public void Directives_should_be_considered_as_unmatched_tokens_when_they_are_not_matched()
        {
            CliDirective directive = new("parse");

            ParseResult result = Parse(new CliOption<bool>("-y"), directive, $"{CliRootCommand.ExecutableName} [nonExisting] -y");

            result.UnmatchedTokens.Should().ContainSingle("[nonExisting]");
        }

        [Fact]
        public void Raw_tokens_still_hold_directives()
        {
            CliDirective directive = new ("parse");

            ParseResult result = Parse(new CliOption<bool>("-y"), directive, "[parse] -y");

            result.FindResultFor(directive).Should().NotBeNull();
            result.Tokens.Should().Contain(t => t.Value == "[parse]");
        }

        [Fact]
        public void Multiple_directives_are_allowed()
        {
            CliRootCommand root = new() { new CliOption<bool>("-y") };
            CliDirective parseDirective = new ("parse");
            CliDirective suggestDirective = new ("suggest");
            CliConfiguration config = new(root);
            config.Directives.Add(parseDirective);
            config.Directives.Add(suggestDirective);

            var result = root.Parse("[parse] [suggest] -y", config);

            result.FindResultFor(parseDirective).Should().NotBeNull();
            result.FindResultFor(suggestDirective).Should().NotBeNull();
        }

        [Fact]
        public void Directives_must_be_the_first_argument()
        {
            CliDirective directive = new("parse");

            ParseResult result = Parse(new CliOption<bool>("-y"), directive, "-y [parse]");

            result.FindResultFor(directive).Should().BeNull();
        }

        [Theory]
        [InlineData("[key:value]", "key", "value")]
        [InlineData("[key:value:more]", "key", "value:more")]
        [InlineData("[key:]", "key", "")]
        public void Directives_can_have_a_value_which_is_everything_after_the_first_colon(
            string wholeText,
            string key,
            string expectedValue)
        {
            CliDirective directive = new(key);

            ParseResult result = Parse(new CliOption<bool>("-y"), directive, $"{wholeText} -y");

            result.FindResultFor(directive).Values.Single().Should().Be(expectedValue);
        }

        [Fact]
        public void Directives_without_a_value_specified_have_no_values()
        {
            CliDirective directive = new("parse");

            ParseResult result = Parse(new CliOption<bool>("-y"), directive, "[parse] -y");

            result.FindResultFor(directive).Values.Should().BeEmpty();
        }

        [Theory]
        [InlineData("[]")]
        [InlineData("[:value]")]
        public void Directives_must_have_a_non_empty_key(string directive)
        {
            CliOption<bool> option = new ("-a");
            CliRootCommand root = new () { option };

            var result = root.Parse($"{directive} -a");

            result.UnmatchedTokens.Should().Contain(directive);
        }

        [Theory]
        [InlineData("[par se]", "[par", "se]")]
        [InlineData("[ parse]", "[", "parse]")]
        [InlineData("[parse ]", "[parse", "]")]
        public void Directives_cannot_contain_spaces(string value, string firstUnmatchedToken, string secondUnmatchedToken)
        {
            Action create = () => new CliDirective(value);
            create.Should().Throw<ArgumentException>();

            CliDirective directive = new("parse");
            ParseResult result = Parse(new CliOption<bool>("-y"), directive, $"{value} -y");
            result.FindResultFor(directive).Should().BeNull();

            result.UnmatchedTokens.Should().BeEquivalentTo(firstUnmatchedToken, secondUnmatchedToken);
        }

        [Fact]
        public void When_a_directive_is_specified_more_than_once_then_its_values_are_aggregated()
        {
            CliDirective directive = new("directive");

            ParseResult result = Parse(new CliOption<bool>("-a"), directive, "[directive:one] [directive:two] -a");

            result.FindResultFor(directive).Values.Should().BeEquivalentTo("one", "two");
        }

        private static ParseResult Parse(CliOption option, CliDirective directive, string commandLine)
        {
            CliRootCommand root = new() { option };
            CliConfiguration config = new(root);
            config.Directives.Add(directive);

            return root.Parse(commandLine, config);
        }
    }
}