using NUnit.Framework;
using LoanAnnuityCalculator.Services;

namespace LoanAnnuityCalculator.Tests
{
    public class AnnuityCalculatorTests
    {
        private AnnuityCalculator _annuityCalculator;

        [SetUp]
        public void Setup()
        {
            _annuityCalculator = new AnnuityCalculator();
        }

        [Test]
        public void CalculateMonthlyAnnuity_ValidInput_ReturnsCorrectValue()
        {
            // Arrange
            double loanAmount = 10000;
            double annualInterestRate = 5; // 5%
            int tenorInMonths = 24; // 2 years

            // Act
            double monthlyAnnuity = _annuityCalculator.CalculateMonthlyAnnuity(loanAmount, annualInterestRate, tenorInMonths);

            // Assert
            Assert.AreEqual(549.86, monthlyAnnuity, 0.01);
        }

        [Test]
        public void CalculateInterestComponent_ValidInput_ReturnsCorrectValue()
        {
            // Arrange
            double loanAmount = 10000;
            double annualInterestRate = 5; // 5%
            int tenorInMonths = 24; // 2 years

            // Act
            double interestComponent = _annuityCalculator.CalculateInterestComponent(loanAmount, annualInterestRate, tenorInMonths);

            // Assert
            Assert.AreEqual(299.59, interestComponent, 0.01);
        }

        [Test]
        public void CalculateCapitalComponent_ValidInput_ReturnsCorrectValue()
        {
            // Arrange
            double loanAmount = 10000;
            double annualInterestRate = 5; // 5%
            int tenorInMonths = 24; // 2 years

            // Act
            double capitalComponent = _annuityCalculator.CalculateCapitalComponent(loanAmount, annualInterestRate, tenorInMonths);

            // Assert
            Assert.AreEqual(10000 - 299.59, capitalComponent, 0.01);
        }
    }
}