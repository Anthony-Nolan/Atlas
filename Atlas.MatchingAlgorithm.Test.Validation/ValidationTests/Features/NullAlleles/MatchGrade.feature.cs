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
    [NUnit.Framework.DescriptionAttribute("Scoring - Expressing Typing vs. Null Allele - Match Grade")]
    public partial class Scoring_ExpressingTypingVs_NullAllele_MatchGradeFeature
    {
        
        private TechTalk.SpecFlow.ITestRunner testRunner;
        
        private string[] _featureTags = ((string[])(null));
        
#line 1 "MatchGrade.feature"
#line hidden
        
        [NUnit.Framework.OneTimeSetUpAttribute()]
        public virtual void FeatureSetup()
        {
            testRunner = TechTalk.SpecFlow.TestRunnerManager.GetTestRunner();
            TechTalk.SpecFlow.FeatureInfo featureInfo = new TechTalk.SpecFlow.FeatureInfo(new System.Globalization.CultureInfo("en-US"), "ValidationTests/Features/NullAlleles", "Scoring - Expressing Typing vs. Null Allele - Match Grade", "  As a member of the search team\r\n  I want search results to correctly handle the" +
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
                TechTalk.SpecFlow.Table table37 = new TechTalk.SpecFlow.Table(new string[] {
                            "A_1",
                            "A_2",
                            "B_1",
                            "B_2",
                            "DRB1_1",
                            "DRB1_2",
                            "C_1",
                            "C_2"});
                table37.AddRow(new string[] {
                            "*01:01",
                            "*02:01",
                            "*08:01",
                            "*08:01",
                            "*07:01",
                            "*07:01",
                            "*01:02",
                            "*01:02"});
#line 7
    testRunner.And("the matching donor has the following HLA:", ((string)(null)), table37, "And ");
#line hidden
                TechTalk.SpecFlow.Table table38 = new TechTalk.SpecFlow.Table(new string[] {
                            "A_1",
                            "A_2",
                            "B_1",
                            "B_2",
                            "DRB1_1",
                            "DRB1_2",
                            "C_1",
                            "C_2"});
                table38.AddRow(new string[] {
                            "*01:01",
                            "*02:01",
                            "*08:01",
                            "*08:01",
                            "*07:01",
                            "*07:01",
                            "*01:02",
                            "*02:52N"});
#line 10
    testRunner.And("the patient has the following HLA:", ((string)(null)), table38, "And ");
#line hidden
#line 13
    testRunner.And("scoring is enabled at locus C", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
#line 14
    testRunner.When("I run a 6/6 search", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
#line 15
    testRunner.Then("the match grade should be molecular-match in position 1 and expressing-vs-null in" +
                        " position 2 of locus C", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
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
                TechTalk.SpecFlow.Table table39 = new TechTalk.SpecFlow.Table(new string[] {
                            "A_1",
                            "A_2",
                            "B_1",
                            "B_2",
                            "DRB1_1",
                            "DRB1_2",
                            "C_1",
                            "C_2"});
                table39.AddRow(new string[] {
                            "*01:01",
                            "*02:01",
                            "*08:01",
                            "*08:01",
                            "*07:01",
                            "*07:01",
                            "*01:02",
                            "*01:02"});
#line 19
    testRunner.And("the matching donor has the following HLA:", ((string)(null)), table39, "And ");
#line hidden
                TechTalk.SpecFlow.Table table40 = new TechTalk.SpecFlow.Table(new string[] {
                            "A_1",
                            "A_2",
                            "B_1",
                            "B_2",
                            "DRB1_1",
                            "DRB1_2",
                            "C_1",
                            "C_2"});
                table40.AddRow(new string[] {
                            "*01:01",
                            "*02:01",
                            "*08:01",
                            "*08:01",
                            "*07:01",
                            "*07:01",
                            "*01:10",
                            "*02:52N"});
#line 22
    testRunner.And("the patient has the following HLA:", ((string)(null)), table40, "And ");
#line hidden
#line 25
    testRunner.And("scoring is enabled at locus C", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
#line 26
    testRunner.When("I run a 6/6 search", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
#line 27
    testRunner.Then("the match grade should be mismatch in position 1 and expressing-vs-null in positi" +
                        "on 2 of locus C", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
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
                TechTalk.SpecFlow.Table table41 = new TechTalk.SpecFlow.Table(new string[] {
                            "A_1",
                            "A_2",
                            "B_1",
                            "B_2",
                            "DRB1_1",
                            "DRB1_2",
                            "C_1",
                            "C_2"});
                table41.AddRow(new string[] {
                            "*01:01",
                            "*02:01",
                            "*08:01",
                            "*08:01",
                            "*07:01",
                            "*07:01",
                            "*01:02",
                            "*01:02"});
#line 31
    testRunner.And("the matching donor has the following HLA:", ((string)(null)), table41, "And ");
#line hidden
                TechTalk.SpecFlow.Table table42 = new TechTalk.SpecFlow.Table(new string[] {
                            "A_1",
                            "A_2",
                            "B_1",
                            "B_2",
                            "DRB1_1",
                            "DRB1_2",
                            "C_1",
                            "C_2"});
                table42.AddRow(new string[] {
                            "*01:01",
                            "*02:01",
                            "*08:01",
                            "*08:01",
                            "*07:01",
                            "*07:01",
                            "*03:03",
                            "*02:52N"});
#line 34
    testRunner.And("the patient has the following HLA:", ((string)(null)), table42, "And ");
#line hidden
#line 37
    testRunner.And("scoring is enabled at locus C", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
#line 38
    testRunner.When("I run a 6/6 search", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
#line 39
    testRunner.Then("the match grade should be mismatch in position 1 and expressing-vs-null in positi" +
                        "on 2 of locus C", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
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
                TechTalk.SpecFlow.Table table43 = new TechTalk.SpecFlow.Table(new string[] {
                            "A_1",
                            "A_2",
                            "B_1",
                            "B_2",
                            "DRB1_1",
                            "DRB1_2",
                            "DPB1_1",
                            "DPB1_2"});
                table43.AddRow(new string[] {
                            "*01:01",
                            "*02:01",
                            "*08:01",
                            "*08:01",
                            "*07:01",
                            "*07:01",
                            "*03:01",
                            "*03:01"});
#line 43
 testRunner.And("the matching donor has the following HLA:", ((string)(null)), table43, "And ");
#line hidden
                TechTalk.SpecFlow.Table table44 = new TechTalk.SpecFlow.Table(new string[] {
                            "A_1",
                            "A_2",
                            "B_1",
                            "B_2",
                            "DRB1_1",
                            "DRB1_2",
                            "DPB1_1",
                            "DPB1_2"});
                table44.AddRow(new string[] {
                            "*01:01",
                            "*02:01",
                            "*08:01",
                            "*08:01",
                            "*07:01",
                            "*07:01",
                            "*08:01",
                            "*64:01N"});
#line 46
 testRunner.And("the patient has the following HLA:", ((string)(null)), table44, "And ");
#line hidden
#line 49
 testRunner.And("scoring is enabled at locus DPB1", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
#line 50
 testRunner.When("I run a 6/6 search", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
#line 51
 testRunner.Then("the match grade should be mismatch in position 1 and expressing-vs-null in positi" +
                        "on 2 of locus DPB1", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
#line hidden
            }
            this.ScenarioCleanup();
        }
    }
}
#pragma warning restore
#endregion