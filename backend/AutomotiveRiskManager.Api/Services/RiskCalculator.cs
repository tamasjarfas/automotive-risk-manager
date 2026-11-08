// Services/RiskCalculator.cs
// Központi, tisztán funkcionális és determinisztikus kockázatszámító osztály.
//
// Szakdolgozati jelentősége:
//   Az autóipari információbiztonsági kockázatértékelés (5×5-ös kockázati mátrix)
//   üzleti logikája egyetlen, jól tesztelhető osztályban összpontosul.
//   A vezérlők (RisksController) SOHA nem fogadnak el a klienstől küldött
//   pontszámot (Score) vagy kockázati szintet (Level), hanem minden létrehozásnál
//   és módosításnál ezt az osztályt hívják meg.
//
// Kockázati mátrix besorolási szabályok (Score = Probability × Impact):
//   - 1–4   pont: Alacsony (RiskLevel.Low)
//   - 5–9   pont: Közepes  (RiskLevel.Medium)
//   - 10–16 pont: Magas    (RiskLevel.High)
//   - 17–25 pont: Kritikus (RiskLevel.Critical)

using AutomotiveRiskManager.Api.Models;

namespace AutomotiveRiskManager.Api.Services;

/// <summary>
/// A kockázatszámítás eredményét összefoglaló értékpár (érték-objektum).
/// </summary>
public readonly record struct RiskEvaluationResult(int Score, RiskLevel Level);

public static class RiskCalculator
{
    public const int MinRating = 1;
    public const int MaxRating = 5;
    public const int MinScore  = MinRating * MinRating; // 1
    public const int MaxScore  = MaxRating * MaxRating; // 25

    /// <summary>
    /// Ellenőrzi, hogy a valószínűség és a hatás értéke az engedélyezett 1–5 tartományba esik-e.
    /// Érvénytelen érték (pl. 0 vagy 6) esetén ArgumentOutOfRangeException kivételt dob.
    /// </summary>
    public static void ValidateInputs(int probability, int impact)
    {
        if (probability < MinRating || probability > MaxRating)
        {
            throw new ArgumentOutOfRangeException(
                nameof(probability),
                probability,
                $"A bekövetkezési valószínűségnek {MinRating} és {MaxRating} közötti egész számnak kell lennie.");
        }

        if (impact < MinRating || impact > MaxRating)
        {
            throw new ArgumentOutOfRangeException(
                nameof(impact),
                impact,
                $"A hatás súlyosságának {MinRating} és {MaxRating} közötti egész számnak kell lennie.");
        }
    }

    /// <summary>
    /// Kiszámítja a kockázati pontszámot: Valószínűség × Hatás (1–25).
    /// </summary>
    public static int CalculateScore(int probability, int impact)
    {
        ValidateInputs(probability, impact);
        return probability * impact;
    }

    /// <summary>
    /// Meghatározza a kockázati szintet a pontszám (1–25) alapján:
    /// 1–4: Low, 5–9: Medium, 10–16: High, 17–25: Critical.
    /// </summary>
    public static RiskLevel DetermineLevel(int score)
    {
        return score switch
        {
            >= 1  and <= 4  => RiskLevel.Low,
            >= 5  and <= 9  => RiskLevel.Medium,
            >= 10 and <= 16 => RiskLevel.High,
            >= 17 and <= 25 => RiskLevel.Critical,
            _ => throw new ArgumentOutOfRangeException(
                nameof(score),
                score,
                $"A kockázati pontszámnak {MinScore} és {MaxScore} közé kell esnie.")
        };
    }

    /// <summary>
    /// Egyetlen lépésben ellenőrzi a bemeneteket, kiszámítja a pontszámot és meghatározza a kockázati szintet.
    /// </summary>
    public static RiskEvaluationResult Evaluate(int probability, int impact)
    {
        int score = CalculateScore(probability, impact);
        RiskLevel level = DetermineLevel(score);
        return new RiskEvaluationResult(score, level);
    }
}

