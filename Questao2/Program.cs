using Newtonsoft.Json;
using Questao2;

public class Program
{
    private static readonly HttpClient client = new HttpClient();
    private const string ApiBaseUrl = "https://jsonmock.hackerrank.com/api/football_matches";

    public static async Task Main()
    {
        string teamName = "Paris Saint-Germain";
        int year = 2013;
        int totalGoals = await getTotalScoredGoals(teamName, year);

        Console.WriteLine("Team "+ teamName +" scored "+ totalGoals.ToString() + " goals in "+ year);

        teamName = "Chelsea";
        year = 2014;
        totalGoals = await getTotalScoredGoals(teamName, year);

        Console.WriteLine("Team " + teamName + " scored " + totalGoals.ToString() + " goals in " + year);

        // Output expected:
        // Team Paris Saint - Germain scored 109 goals in 2013
        // Team Chelsea scored 92 goals in 2014
    }

    /// <summary>
    /// Method that calculates the total number of goals scored by a team in a specific year
    /// </summary>
    /// <param name="team">Team name</param>
    /// <param name="year">Year of the season</param>
    /// <returns>Total goals scored by the team</returns>
    public static async Task<int> getTotalScoredGoals(string team, int year)
    {
        int totalGoals = 0;

        // Look for goals when the team is team1 (home team) 
        totalGoals += await GetGoalsForTeam(team, year, "team1", "team1goals");

        // Look for goals when the team is team2 (visiting team)
        totalGoals += await GetGoalsForTeam(team, year, "team2", "team2goals");

        return totalGoals;
    }

    /// <summary>
    /// Auxiliar method that searches for a team's goals
    /// </summary>
    /// <param name="team">Team name</param>
    /// <param name="year">Year of the sesaon</param>
    /// <param name="teamParam">API Param (team1 or team2)</param>
    /// <param name="goalsParam">Goals scored Param (team1 or team2)</param>
    /// <returns>Sum of the goals</returns>
    private static async Task<int> GetGoalsForTeam(string team, int year, string teamParam, string goalsParam)
    {
        int total = 0;
        int page = 1;
        int totalPages;
        bool hasMorePages = true;

        while (hasMorePages)
        {
            string requestUrl = $"{ApiBaseUrl}?year{year}&{teamParam}={team}&page={page}";
            HttpResponseMessage response = await client.GetAsync(requestUrl);

            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(responseBody);

                totalPages = apiResponse.Total_Pages;

                foreach (var match in apiResponse.Data)
                {
                    if (match.TryGetValue("year", out var yearValue) &&
                        int.TryParse(yearValue?.ToString(), out int matchYear) &&
                        matchYear == year &&
                        match.TryGetValue(goalsParam, out var goalsValue) &&
                        int.TryParse(goalsValue?.ToString(), out int goals))
                    {
                        total += goals;
                    }
                }

                hasMorePages = page < totalPages;
                page++;
            }
            else
            {
                hasMorePages = false;
            }
        }

        return total;
    }

}