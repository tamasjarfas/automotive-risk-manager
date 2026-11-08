// RiskCalculatorTests.cs
// Egységtesztek a központi RiskCalculator kockázatértékelő osztályhoz.
//
// Futtatás GitHub Codespacesben:
//   dotnet test backend/AutomotiveRiskManager.Tests
//
// Ellenőrzött határértékek (5×5-ös kockázati mátrix):
//   - 1 × 1 = 1   → Low (Alacsony)
//   - 2 × 2 = 4   → Low (Alacsony)
//   - 1 × 5 = 5   → Medium (Közepes)
//   - 3 × 3 = 9   → Medium (Közepes)
//   - 2 × 5 = 10  → High (Magas)
//   - 4 × 4 = 16  → High (Magas)
//   - 4 × 5 = 20  → Critical (Kritikus)
//   - 5 × 5 = 25  → Critical (Kritikus)
//   - 0 vagy 6    → ArgumentOutOfRangeException (validációs hiba)

using AutomotiveRiskManager.Api.Models;
using AutomotiveRiskManager.Api.Services;
using Xunit;

namespace AutomotiveRiskManager.Tests;

public class RiskCalculatorTests
{
    [Theory]
    [InlineData(1, 1, 1,  RiskLevel.Low)]       // Alsó határ (Alacsony: 1–4)
    [InlineData(2, 2, 4,  RiskLevel.Low)]       // Alacsony felső határa (4)
    [InlineData(1, 5, 5,  RiskLevel.Medium)]    // Közepes alsó határa (5)
    [InlineData(3, 3, 9,  RiskLevel.Medium)]    // Közepes felső határa (9)
    [InlineData(2, 5, 10, RiskLevel.High)]      // Magas alsó határa (10)
    [InlineData(4, 4, 16, RiskLevel.High)]      // Magas felső határa (16)
    [InlineData(4, 5, 20, RiskLevel.Critical)]  // Kritikus tartomány (17–25)
    [InlineData(5, 5, 25, RiskLevel.Critical)]  // Felső határ (25)
    public void Evaluate_ValidBoundaryInputs_ReturnsExpectedScoreAndLevel(
        int probability,
        int impact,
        int expectedScore,
        RiskLevel expectedLevel)
    {
        // Act
        RiskEvaluationResult result = RiskCalculator.Evaluate(probability, impact);

        // Assert
        Assert.Equal(expectedScore, result.Score);
        Assert.Equal(expectedLevel, result.Level);
    }

    [Theory]
    [InlineData(0, 3)]
    [InlineData(6, 3)]
    [InlineData(-1, 2)]
    [InlineData(3, 0)]
    [InlineData(3, 6)]
    [InlineData(0, 0)]
    [InlineData(6, 6)]
    public void Evaluate_OutOfRangeProbabilityOrImpact_ThrowsArgumentOutOfRangeException(
        int probability,
        int impact)
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RiskCalculator.Evaluate(probability, impact));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(26)]
    [InlineData(-5)]
    public void DetermineLevel_OutOfRangeScore_ThrowsArgumentOutOfRangeException(int invalidScore)
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RiskCalculator.DetermineLevel(invalidScore));
    }
}

