﻿// ------------------------------------------------------------------------------
//  <auto-generated>
//      This code was generated by SpecFlow (https://www.specflow.org/).
//      SpecFlow Version:3.5.0.0
//      SpecFlow Generator Version:3.5.0.0
// 
//      Changes to this file may cause incorrect behavior and will be lost if
//      the code is regenerated.
//  </auto-generated>
// ------------------------------------------------------------------------------
#region Designer generated code
#pragma warning disable
namespace Atlas.MatchingAlgorithm.Test.Validation.ValidationTests.Features.NullAlleles
{
    using TechTalk.SpecFlow;
    using System;
    using System.Linq;
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("TechTalk.SpecFlow", "3.5.0.0")]
    [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [NUnit.Framework.TestFixtureAttribute()]
    [NUnit.Framework.DescriptionAttribute("Scoring - Expressing Typing vs. Null Allele - IsAntigenMatch")]
    public partial class Scoring_ExpressingTypingVs_NullAllele_IsAntigenMatchFeature
    {
        
        private TechTalk.SpecFlow.ITestRunner testRunner;
        
        private string[] _featureTags = ((string[])(null));
        
#line 1 "IsAntigenMatch.feature"
#line hidden
        
        [NUnit.Framework.OneTimeSetUpAttribute()]
        public virtual void FeatureSetup()
        {
            testRunner = TechTalk.SpecFlow.TestRunnerManager.GetTestRunner();
            TechTalk.SpecFlow.FeatureInfo featureInfo = new TechTalk.SpecFlow.FeatureInfo(new System.Globalization.CultureInfo("en-US"), "ValidationTests/Features/NullAlleles", "Scoring - Expressing Typing vs. Null Allele - IsAntigenMatch", "  As a member of the search team\r\n  I want search results to correctly handle the" +
                    " scoring of a locus that contains a null allele.", ProgrammingLanguage.CSharp, ((string[])(null)));
            testRunner.OnFeatureStart(featureInfo);
        }
        
        [NUnit.Framework.OneTimeTearDownAttribute()]
        public virtual void FeatureTearDown()
        {
            testRunner.OnFeatureEnd();
            testRunner = null;
        }
        
        [NUnit.Framework.SetUpAttribute()]
        public virtual void TestInitialize()
        {
        }
        
        [NUnit.Framework.TearDownAttribute()]
        public virtual void TestTearDown()
        {
            testRunner.OnScenarioEnd();
        }
        
        public virtual void ScenarioInitialize(TechTalk.SpecFlow.ScenarioInfo scenarioInfo)
        {
            testRunner.OnScenarioInitialize(scenarioInfo);
            testRunner.ScenarioContext.ScenarioContainer.RegisterInstanceAs<NUnit.Framework.TestContext>(NUnit.Framework.TestContext.CurrentContext);
        }
        
        public virtual void ScenarioStart()
        {
            testRunner.OnScenarioStart();
        }
        
        public virtual void ScenarioCleanup()
        {
            testRunner.CollectScenarioErrors();
        }
        
        [NUnit.Framework.TestAttribute()]
        [NUnit.Framework.DescriptionAttribute("Homozygous locus vs Null-allele containing locus are matched at expressing typing" +
            "")]
        public virtual void HomozygousLocusVsNull_AlleleContainingLocusAreMatchedAtExpressingTyping()
        {
            string[] tagsOfScenario = ((string[])(null));
            System.Collections.Specialized.OrderedDictionary argumentsOfScenario = new System.Collections.Specialized.OrderedDictionary();
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Homozygous locus vs Null-allele containing locus are matched at expressing typing" +
                    "", null, tagsOfScenario, argumentsOfScenario);
#line 5
  this.ScenarioInitialize(scenarioInfo);
#line hidden
            bool isScenarioIgnored = default(bool);
            bool isFeatureIgnored = default(bool);
            if ((tagsOfScenario != null))
            {
                isScenarioIgnored = tagsOfScenario.Where(__entry => __entry != null).Where(__entry => String.Equals(__entry, "ignore", StringComparison.CurrentCultureIgnoreCase)).Any();
            }
            if ((this._featureTags != null))
            {
                isFeatureIgnored = this._featureTags.Where(__entry => __entry != null).Where(__entry => String.Equals(__entry, "ignore", StringComparison.CurrentCultureIgnoreCase)).Any();
            }
            if ((isScenarioIgnored || isFeatureIgnored))
            {
                testRunner.SkipScenario();
            }
            else
            {
                this.ScenarioStart();
#line 6
    testRunner.Given("a patient has a match", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line hidden
                TechTalk.SpecFlow.Table table5 = new TechTalk.SpecFlow.Table(new string[] {
                            "A_1",
                            "A_2",
                            "B_1",
                            "B_2",
                            "DRB1_1",
                            "DRB1_2"});
                table5.AddRow(new string[] {
                            "*02:01",
                            "*02:01",
                            "*08:01",
                            "*08:01",
                            "*07:01",
                            "*07:01"});
#line 7
    testRunner.And("the matching donor has the following HLA:", ((string)(null)), table5, "And ");
#line hidden
                TechTalk.SpecFlow.Table table6 = new TechTalk.SpecFlow.Table(new string[] {
                            "A_1",
                            "A_2",
                            "B_1",
                            "B_2",
                            "DRB1_1",
                            "DRB1_2"});
                table6.AddRow(new string[] {
                            "*02:01",
                            "*01:01N",
                            "*08:01",
                            "*08:01",
                            "*07:01",
                            "*07:01"});
#line 10
    testRunner.And("the patient has the following HLA:", ((string)(null)), table6, "And ");
#line hidden
#line 13
    testRunner.And("scoring is enabled at locus A", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
#line 14
    testRunner.When("I run a 6/6 search", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
#line 15
    testRunner.Then("antigen match should be true at locus A at both positions", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
#line hidden
            }
            this.ScenarioCleanup();
        }
        
        [NUnit.Framework.TestAttribute()]
        [NUnit.Framework.DescriptionAttribute("Homozygous locus vs Null-allele containing locus are allele mismatched at express" +
            "ing typing")]
        public virtual void HomozygousLocusVsNull_AlleleContainingLocusAreAlleleMismatchedAtExpressingTyping()
        {
            string[] tagsOfScenario = ((string[])(null));
            System.Collections.Specialized.OrderedDictionary argumentsOfScenario = new System.Collections.Specialized.OrderedDictionary();
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Homozygous locus vs Null-allele containing locus are allele mismatched at express" +
                    "ing typing", null, tagsOfScenario, argumentsOfScenario);
#line 17
  this.ScenarioInitialize(scenarioInfo);
#line hidden
            bool isScenarioIgnored = default(bool);
            bool isFeatureIgnored = default(bool);
            if ((tagsOfScenario != null))
            {
                isScenarioIgnored = tagsOfScenario.Where(__entry => __entry != null).Where(__entry => String.Equals(__entry, "ignore", StringComparison.CurrentCultureIgnoreCase)).Any();
            }
            if ((this._featureTags != null))
            {
                isFeatureIgnored = this._featureTags.Where(__entry => __entry != null).Where(__entry => String.Equals(__entry, "ignore", StringComparison.CurrentCultureIgnoreCase)).Any();
            }
            if ((isScenarioIgnored || isFeatureIgnored))
            {
                testRunner.SkipScenario();
            }
            else
            {
                this.ScenarioStart();
#line 18
    testRunner.Given("a patient has a match", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line hidden
                TechTalk.SpecFlow.Table table7 = new TechTalk.SpecFlow.Table(new string[] {
                            "A_1",
                            "A_2",
                            "B_1",
                            "B_2",
                            "DRB1_1",
                            "DRB1_2"});
                table7.AddRow(new string[] {
                            "*02:01",
                            "*02:01",
                            "*08:01",
                            "*08:01",
                            "*07:01",
                            "*07:01"});
#line 19
    testRunner.And("the matching donor has the following HLA:", ((string)(null)), table7, "And ");
#line hidden
                TechTalk.SpecFlow.Table table8 = new TechTalk.SpecFlow.Table(new string[] {
                            "A_1",
                            "A_2",
                            "B_1",
                            "B_2",
                            "DRB1_1",
                            "DRB1_2"});
                table8.AddRow(new string[] {
                            "*02:02",
                            "*01:01N",
                            "*08:01",
                            "*08:01",
                            "*07:01",
                            "*07:01"});
#line 22
    testRunner.And("the patient has the following HLA:", ((string)(null)), table8, "And ");
#line hidden
#line 25
    testRunner.And("scoring is enabled at locus A", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
#line 26
    testRunner.When("I run a 4/6 search", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
#line 27
    testRunner.Then("antigen match should be true at locus A at both positions", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
#line hidden
            }
            this.ScenarioCleanup();
        }
        
        [NUnit.Framework.TestAttribute()]
        [NUnit.Framework.DescriptionAttribute("Homozygous locus vs Null-allele containing locus are antigen mismatched at expres" +
            "sing typing")]
        public virtual void HomozygousLocusVsNull_AlleleContainingLocusAreAntigenMismatchedAtExpressingTyping()
        {
            string[] tagsOfScenario = ((string[])(null));
            System.Collections.Specialized.OrderedDictionary argumentsOfScenario = new System.Collections.Specialized.OrderedDictionary();
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Homozygous locus vs Null-allele containing locus are antigen mismatched at expres" +
                    "sing typing", null, tagsOfScenario, argumentsOfScenario);
#line 29
  this.ScenarioInitialize(scenarioInfo);
#line hidden
            bool isScenarioIgnored = default(bool);
            bool isFeatureIgnored = default(bool);
            if ((tagsOfScenario != null))
            {
                isScenarioIgnored = tagsOfScenario.Where(__entry => __entry != null).Where(__entry => String.Equals(__entry, "ignore", StringComparison.CurrentCultureIgnoreCase)).Any();
            }
            if ((this._featureTags != null))
            {
                isFeatureIgnored = this._featureTags.Where(__entry => __entry != null).Where(__entry => String.Equals(__entry, "ignore", StringComparison.CurrentCultureIgnoreCase)).Any();
            }
            if ((isScenarioIgnored || isFeatureIgnored))
            {
                testRunner.SkipScenario();
            }
            else
            {
                this.ScenarioStart();
#line 30
    testRunner.Given("a patient has a match", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line hidden
                TechTalk.SpecFlow.Table table9 = new TechTalk.SpecFlow.Table(new string[] {
                            "A_1",
                            "A_2",
                            "B_1",
                            "B_2",
                            "DRB1_1",
                            "DRB1_2"});
                table9.AddRow(new string[] {
                            "*02:01",
                            "*02:01",
                            "*08:01",
                            "*08:01",
                            "*07:01",
                            "*07:01"});
#line 31
    testRunner.And("the matching donor has the following HLA:", ((string)(null)), table9, "And ");
#line hidden
                TechTalk.SpecFlow.Table table10 = new TechTalk.SpecFlow.Table(new string[] {
                            "A_1",
                            "A_2",
                            "B_1",
                            "B_2",
                            "DRB1_1",
                            "DRB1_2"});
                table10.AddRow(new string[] {
                            "*03:01",
                            "*01:01N",
                            "*08:01",
                            "*08:01",
                            "*07:01",
                            "*07:01"});
#line 34
    testRunner.And("the patient has the following HLA:", ((string)(null)), table10, "And ");
#line hidden
#line 37
    testRunner.And("scoring is enabled at locus A", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
#line 38
    testRunner.When("I run a 4/6 search", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
#line 39
    testRunner.Then("antigen match should be false at locus A at both positions", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
#line hidden
            }
            this.ScenarioCleanup();
        }
        
        [NUnit.Framework.TestAttribute()]
        [NUnit.Framework.DescriptionAttribute("Homozygous DPB1 vs Null-allele containing DPB1 are permissively mismatched at exp" +
            "ressing typing")]
        public virtual void HomozygousDPB1VsNull_AlleleContainingDPB1ArePermissivelyMismatchedAtExpressingTyping()
        {
            string[] tagsOfScenario = ((string[])(null));
            System.Collections.Specialized.OrderedDictionary argumentsOfScenario = new System.Collections.Specialized.OrderedDictionary();
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Homozygous DPB1 vs Null-allele containing DPB1 are permissively mismatched at exp" +
                    "ressing typing", null, tagsOfScenario, argumentsOfScenario);
#line 41
  this.ScenarioInitialize(scenarioInfo);
#line hidden
            bool isScenarioIgnored = default(bool);
            bool isFeatureIgnored = default(bool);
            if ((tagsOfScenario != null))
            {
                isScenarioIgnored = tagsOfScenario.Where(__entry => __entry != null).Where(__entry => String.Equals(__entry, "ignore", StringComparison.CurrentCultureIgnoreCase)).Any();
            }
            if ((this._featureTags != null))
            {
                isFeatureIgnored = this._featureTags.Where(__entry => __entry != null).Where(__entry => String.Equals(__entry, "ignore", StringComparison.CurrentCultureIgnoreCase)).Any();
            }
            if ((isScenarioIgnored || isFeatureIgnored))
            {
                testRunner.SkipScenario();
            }
            else
            {
                this.ScenarioStart();
#line 42
 testRunner.Given("a patient has a match", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line hidden
                TechTalk.SpecFlow.Table table11 = new TechTalk.SpecFlow.Table(new string[] {
                            "A_1",
                            "A_2",
                            "B_1",
                            "B_2",
                            "DRB1_1",
                            "DRB1_2",
                            "DPB1_1",
                            "DPB1_2"});
                table11.AddRow(new string[] {
                            "*01:01",
                            "*02:01",
                            "*08:01",
                            "*08:01",
                            "*07:01",
                            "*07:01",
                            "*03:01",
                            "*03:01"});
#line 43
 testRunner.And("the matching donor has the following HLA:", ((string)(null)), table11, "And ");
#line hidden
                TechTalk.SpecFlow.Table table12 = new TechTalk.SpecFlow.Table(new string[] {
                            "A_1",
                            "A_2",
                            "B_1",
                            "B_2",
                            "DRB1_1",
                            "DRB1_2",
                            "DPB1_1",
                            "DPB1_2"});
                table12.AddRow(new string[] {
                            "*01:01",
                            "*02:01",
                            "*08:01",
                            "*08:01",
                            "*07:01",
                            "*07:01",
                            "*08:01",
                            "*64:01N"});
#line 46
 testRunner.And("the patient has the following HLA:", ((string)(null)), table12, "And ");
#line hidden
#line 49
 testRunner.And("scoring is enabled at locus DPB1", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
#line 50
 testRunner.When("I run a 6/6 search", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
#line 51
 testRunner.Then("antigen match should be false at locus DPB1 at both positions", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
#line hidden
            }
            this.ScenarioCleanup();
        }
    }
}
#pragma warning restore
#endregion
