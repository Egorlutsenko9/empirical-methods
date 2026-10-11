using System.Globalization;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;
CultureInfo.CurrentCulture = new CultureInfo("uk-UA");
CultureInfo.CurrentUICulture = new CultureInfo("uk-UA");

Console.WriteLine("Лабораторна робота №1");
Console.WriteLine("Тема: Дискретні випадкові величини та їх застосування");
Console.WriteLine("Завдання №1: аналіз середнього виграшу в лотереї");

Console.WriteLine();
Console.WriteLine("Оберіть режим роботи:");
Console.WriteLine("1 - готова лотерея \"КОРСАР\"");
Console.WriteLine("2 - власні дані");

int mode = ReadInt("Ваш вибір: ", 1, 2);
LotteryData lottery = mode == 1 ? CreateKorsarLottery() : ReadCustomLottery();

List<DistributionItem> distribution = BuildDistribution(lottery);
PrintResults(lottery, distribution);

Console.WriteLine("Роботу програми завершено.");

static LotteryData CreateKorsarLottery()
{
    return new LotteryData(
        Name: "КОРСАР",
        TicketPrice: 30m,
        TotalTickets: 1_000_000,
        PrizeCategories:
        [
            new PrizeCategory(603_750m, 1),
            new PrizeCategory(3_000m, 300),
            new PrizeCategory(500m, 900),
            new PrizeCategory(200m, 3_000),
            new PrizeCategory(100m, 30_000),
            new PrizeCategory(60m, 90_000),
            new PrizeCategory(30m, 160_000)
        ],
        Description: "Навчальний приклад із методички: вартість білета 30 грн, загальна кількість білетів 1 000 000. У розподілі задано виграшні категорії 603750, 3000, 500, 200, 100, 60 і 30 грн; решта білетів мають виграш 0 грн.");
}

static LotteryData ReadCustomLottery()
{
    Console.WriteLine();
    Console.WriteLine("Введення власної лотереї");

    string name = ReadNonEmptyString("Назва лотереї: ");
    decimal ticketPrice = ReadDecimal("Вартість одного білета, грн: ", 0m, allowZero: true);
    int totalTickets = ReadInt("Загальна кількість білетів: ", 1, int.MaxValue);
    int maxCategoryCount = Math.Min(totalTickets, 100);
    int categoryCount = ReadInt($"Кількість різних виграшних категорій (0-{maxCategoryCount}): ", 0, maxCategoryCount);

    List<PrizeCategory> categories = [];
    HashSet<decimal> usedPrizes = [];
    long winningTicketCount = 0;

    for (int i = 1; i <= categoryCount; i++)
    {
        Console.WriteLine();
        Console.WriteLine($"Категорія №{i}");

        decimal prize = ReadUniquePrize(usedPrizes);
        long remainingCategories = categoryCount - i;
        int maxTicketsForCategory = (int)(totalTickets - winningTicketCount - remainingCategories);
        int count = ReadInt($"Кількість білетів із таким виграшем (1-{maxTicketsForCategory}): ", 1, maxTicketsForCategory);

        winningTicketCount += count;

        categories.Add(new PrizeCategory(prize, count));
        usedPrizes.Add(prize);
    }

    return new LotteryData(name, ticketPrice, totalTickets, categories, Description: null);
}

static decimal ReadUniquePrize(HashSet<decimal> usedPrizes)
{
    while (true)
    {
        decimal prize = ReadDecimal("Сума виграшу, грн: ", 0m, allowZero: false);

        if (usedPrizes.Contains(prize))
        {
            Console.WriteLine("Помилка: така виграшна категорія вже існує. Введіть іншу суму.");
            continue;
        }

        return prize;
    }
}

static List<DistributionItem> BuildDistribution(LotteryData lottery)
{
    List<DistributionItem> distribution = [];

    foreach (PrizeCategory category in lottery.PrizeCategories.OrderByDescending(category => category.Prize))
    {
        // P(X = x) = кількість білетів з виграшем x / загальна кількість білетів.
        decimal probability = (decimal)category.TicketCount / lottery.TotalTickets;
        distribution.Add(new DistributionItem(category.Prize, category.TicketCount, probability));
    }

    long winningTickets = lottery.PrizeCategories.Sum(category => (long)category.TicketCount);
    long losingTickets = lottery.TotalTickets - winningTickets;

    // Програшні білети додаються до закону розподілу як подія X = 0.
    decimal losingProbability = (decimal)losingTickets / lottery.TotalTickets;
    distribution.Add(new DistributionItem(0m, losingTickets, losingProbability));

    return distribution;
}

static void PrintResults(LotteryData lottery, List<DistributionItem> distribution)
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine($"Лотерея: {lottery.Name}");

    if (!string.IsNullOrWhiteSpace(lottery.Description))
    {
        Console.WriteLine($"Опис правил: {lottery.Description}");
    }

    Console.WriteLine($"Вартість білета: {FormatMoney(lottery.TicketPrice)} грн");
    Console.WriteLine($"Загальна кількість білетів: {lottery.TotalTickets:N0}");
    Console.WriteLine();
    Console.WriteLine("Закон розподілу випадкової величини X - суми виграшу");
    Console.WriteLine(new string('-', 78));
    Console.WriteLine($"{"Виграш, грн",15} | {"Кількість білетів",20} | {"Ймовірність",18}");
    Console.WriteLine(new string('-', 78));

    foreach (DistributionItem item in distribution)
    {
        Console.WriteLine($"{FormatMoney(item.Prize),15} | {item.TicketCount,20:N0} | {FormatProbability(item.Probability),18}");
    }

    Console.WriteLine(new string('-', 78));

    decimal probabilitySum = distribution.Sum(item => item.Probability);
    decimal expectedValue = CalculateExpectedValue(distribution);
    decimal netResult = expectedValue - lottery.TicketPrice;

    Console.WriteLine($"Сума ймовірностей: {probabilitySum:F10}");
    Console.WriteLine($"Перевірка: {(Math.Abs(probabilitySum - 1m) <= 0.0000000001m ? "сума ймовірностей дорівнює 1" : "сума ймовірностей НЕ дорівнює 1")}");
    Console.WriteLine();
    Console.WriteLine("Формула математичного сподівання:");
    Console.WriteLine("M(X) = Σ(xᵢ × pᵢ)");
    Console.WriteLine($"M(X) = {BuildExpectedValueFormula(distribution)}");
    Console.WriteLine($"M(X) = {FormatMoney(expectedValue)} грн");
    Console.WriteLine();
    Console.WriteLine($"Середній виграш: {FormatMoney(expectedValue)} грн");
    Console.WriteLine("Очікуваний чистий фінансовий результат:");
    Console.WriteLine($"R = M(X) - C = {FormatMoney(expectedValue)} - {FormatMoney(lottery.TicketPrice)} = {FormatMoney(netResult)} грн");
    Console.WriteLine($"{BuildNetResultLabel(netResult)}: {FormatMoney(Math.Abs(netResult))} грн");
    Console.WriteLine();
    Console.WriteLine($"Висновок: {BuildConclusion(netResult)}");
    Console.WriteLine(new string('=', 78));
}

static decimal CalculateExpectedValue(List<DistributionItem> distribution)
{
    // M(X) = сума добутків можливого виграшу та його ймовірності.
    return distribution.Sum(item => item.Prize * item.Probability);
}

static string BuildExpectedValueFormula(List<DistributionItem> distribution)
{
    return string.Join(" + ", distribution.Select(item => $"{FormatMoney(item.Prize)}×{FormatProbability(item.Probability)}"));
}

static string BuildConclusion(decimal netResult)
{
    if (netResult < 0)
    {
        return "участь у середньому збиткова.";
    }

    if (netResult > 0)
    {
        return "очікуваний результат додатний.";
    }

    return "середній результат беззбитковий.";
}

static string BuildNetResultLabel(decimal netResult)
{
    if (netResult < 0)
    {
        return "Очікуваний чистий збиток";
    }

    if (netResult > 0)
    {
        return "Очікуваний чистий прибуток";
    }

    return "Очікуваний чистий результат";
}

static string ReadNonEmptyString(string prompt)
{
    while (true)
    {
        Console.Write(prompt);
        string? input = Console.ReadLine();

        if (!string.IsNullOrWhiteSpace(input))
        {
            return input.Trim();
        }

        Console.WriteLine("Помилка: значення не може бути порожнім.");
    }
}

static int ReadInt(string prompt, int minValue, int maxValue)
{
    while (true)
    {
        Console.Write(prompt);
        string? input = Console.ReadLine();

        if (int.TryParse(input, NumberStyles.Integer, CultureInfo.CurrentCulture, out int value)
            && value >= minValue
            && value <= maxValue)
        {
            return value;
        }

        Console.WriteLine($"Помилка: введіть ціле число від {minValue:N0} до {maxValue:N0}.");
    }
}

static decimal ReadDecimal(string prompt, decimal minValue, bool allowZero)
{
    while (true)
    {
        Console.Write(prompt);
        string? input = Console.ReadLine();

        if (TryParseDecimal(input, out decimal value) && value >= minValue && (allowZero || value != 0m))
        {
            return value;
        }

        string zeroText = allowZero ? "дозволено" : "заборонено";
        Console.WriteLine($"Помилка: введіть число не менше {FormatMoney(minValue)}; нульове значення {zeroText}.");
    }
}

static bool TryParseDecimal(string? input, out decimal value)
{
    value = 0m;

    if (string.IsNullOrWhiteSpace(input))
    {
        return false;
    }

    string normalizedInput = input.Trim().Replace(" ", string.Empty);

    if (normalizedInput.Contains(',') && normalizedInput.Contains('.'))
    {
        return false;
    }

    normalizedInput = normalizedInput.Replace(',', '.');
    return decimal.TryParse(normalizedInput, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value);
}

static string FormatMoney(decimal value)
{
    return value.ToString("0.############################", CultureInfo.CurrentCulture);
}

static string FormatProbability(decimal value)
{
    return value.ToString("0.############################", CultureInfo.CurrentCulture);
}

record PrizeCategory(decimal Prize, int TicketCount);

record LotteryData(
    string Name,
    decimal TicketPrice,
    int TotalTickets,
    List<PrizeCategory> PrizeCategories,
    string? Description);

record DistributionItem(decimal Prize, long TicketCount, decimal Probability);
