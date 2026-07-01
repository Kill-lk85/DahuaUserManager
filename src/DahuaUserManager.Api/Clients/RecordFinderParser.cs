namespace DahuaUserManager.Api.Clients;

public class RecordFinderParser
{
    public List<AccessControlCard> ParseCards(string response)
    {
        var cards = new Dictionary<int, AccessControlCard>();

        foreach (string line in response.Replace("\r\n", "\n").Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (!line.StartsWith("records["))
                continue;

            int index1 = line.IndexOf('[');
            int index2 = line.IndexOf(']');

            if (index1 < 0 || index2 < 0)
                continue;

            if (!int.TryParse(
                line.Substring(index1 + 1, index2 - index1 - 1),
                out int recordIndex))
                continue;

            if (!cards.TryGetValue(recordIndex, out AccessControlCard? card))
            {
                card = new AccessControlCard();
                cards.Add(recordIndex, card);
            }

            int eq = line.IndexOf('=');

            if (eq < 0)
                continue;

            string key = line.Substring(index2 + 2, eq - index2 - 2);
            string value = line[(eq + 1)..];

            switch (key)
            {
                case "RecNo":
                    int.TryParse(value, out int recNo);
                    card.RecNo = recNo;
                    break;

                case "UserID":
                    card.UserId = value;
                    break;

                case "CardName":
                    card.CardName = value;
                    break;

                case "CardNo":
                    card.CardNo = value;
                    break;

                case "CardStatus":
                    int.TryParse(value, out int status);
                    card.CardStatus = status;
                    break;

                case "IsValid":
                    card.IsValid = value.Equals("true", StringComparison.OrdinalIgnoreCase);
                    break;

                case "ValidDateStart":
                    card.ValidDateStart = value;
                    break;

                case "ValidDateEnd":
                    card.ValidDateEnd = value;
                    break;
            }
        }

        return cards.Values.OrderBy(c => c.RecNo).ToList();
    }
}