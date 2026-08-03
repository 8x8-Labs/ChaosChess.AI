using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Simulator.Balance
{
    public static class BalanceSimulationScenarioJsonLoader
    {
        public static BalanceSimulationScenario LoadFromFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Scenario path cannot be empty.", nameof(path));
            }

            return Load(File.ReadAllText(path));
        }

        public static BalanceSimulationScenario Load(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("Scenario JSON cannot be empty.", nameof(json));
            }

            var options = new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            };

            using JsonDocument document = JsonDocument.Parse(json, options);
            JsonElement root = document.RootElement;

            return new BalanceSimulationScenario(
                RequiredString(root, "scenarioId"),
                RequiredInt(root, "schemaVersion"),
                RequiredString(root, "startingFen"),
                RequiredColor(root, "actor"),
                ReadCards(root),
                ReadTileEffects(root),
                ReadEngineObservation(root),
                RequiredString(root, "scenarioGroup"),
                OptionalEnum(root, "expectedBehavior", BalanceExpectedBehavior.Unspecified));
        }

        private static IReadOnlyList<BalanceScenarioCard> ReadCards(JsonElement root)
        {
            if (!root.TryGetProperty("cards", out JsonElement cardsElement) ||
                cardsElement.ValueKind == JsonValueKind.Null)
            {
                return Array.Empty<BalanceScenarioCard>();
            }

            EnsureArray(cardsElement, "cards");
            var cards = new List<BalanceScenarioCard>();

            foreach (JsonElement cardElement in cardsElement.EnumerateArray())
            {
                cards.Add(new BalanceScenarioCard(
                    RequiredString(cardElement, "cardId"),
                    RequiredString(cardElement, "category"),
                    RequiredInt(cardElement, "remainingUses")));
            }

            return cards.AsReadOnly();
        }

        private static IReadOnlyList<BalanceScenarioTileEffect> ReadTileEffects(JsonElement root)
        {
            if (!root.TryGetProperty("tileEffects", out JsonElement effectsElement) ||
                effectsElement.ValueKind == JsonValueKind.Null)
            {
                return Array.Empty<BalanceScenarioTileEffect>();
            }

            EnsureArray(effectsElement, "tileEffects");
            var effects = new List<BalanceScenarioTileEffect>();

            foreach (JsonElement effectElement in effectsElement.EnumerateArray())
            {
                effects.Add(new BalanceScenarioTileEffect(
                    RequiredString(effectElement, "id"),
                    RequiredString(effectElement, "effectType"),
                    RequiredSquare(effectElement, "square"),
                    RequiredColor(effectElement, "owner"),
                    RequiredInt(effectElement, "remainingTurns"),
                    OptionalSquare(effectElement, "destinationSquare"),
                    OptionalInt(effectElement, "sharedRemainingUses")));
            }

            return effects.AsReadOnly();
        }

        private static BalanceEngineObservation ReadEngineObservation(JsonElement root)
        {
            if (!root.TryGetProperty("engineObservation", out JsonElement observationElement) ||
                observationElement.ValueKind == JsonValueKind.Null)
            {
                return new BalanceEngineObservation();
            }

            if (!observationElement.TryGetProperty("moves", out JsonElement movesElement) ||
                movesElement.ValueKind == JsonValueKind.Null)
            {
                return new BalanceEngineObservation();
            }

            EnsureArray(movesElement, "engineObservation.moves");
            var moves = new List<MoveCandidate>();

            foreach (JsonElement moveElement in movesElement.EnumerateArray())
            {
                moves.Add(new MoveCandidate(
                    RequiredString(moveElement, "uciMove"),
                    RequiredInt(moveElement, "scoreCentipawns"),
                    OptionalInt(moveElement, "mateIn")));
            }

            return new BalanceEngineObservation(moves);
        }

        private static string RequiredString(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out JsonElement property) ||
                property.ValueKind != JsonValueKind.String)
            {
                throw new FormatException("Missing or invalid string property: " + propertyName);
            }

            return property.GetString() ?? string.Empty;
        }

        private static int RequiredInt(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out JsonElement property) ||
                !property.TryGetInt32(out int value))
            {
                throw new FormatException("Missing or invalid integer property: " + propertyName);
            }

            return value;
        }

        private static int? OptionalInt(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out JsonElement property) ||
                property.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            if (!property.TryGetInt32(out int value))
            {
                throw new FormatException("Invalid integer property: " + propertyName);
            }

            return value;
        }

        private static PieceColor RequiredColor(JsonElement element, string propertyName)
        {
            return RequiredEnum<PieceColor>(element, propertyName);
        }

        private static Square RequiredSquare(JsonElement element, string propertyName)
        {
            return Square.Parse(RequiredString(element, propertyName));
        }

        private static Square? OptionalSquare(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out JsonElement property) ||
                property.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            if (property.ValueKind != JsonValueKind.String)
            {
                throw new FormatException("Invalid square property: " + propertyName);
            }

            return Square.Parse(property.GetString() ?? string.Empty);
        }

        private static T RequiredEnum<T>(JsonElement element, string propertyName)
            where T : struct
        {
            string value = RequiredString(element, propertyName);

            if (!Enum.TryParse(value, ignoreCase: true, out T parsed))
            {
                throw new FormatException("Invalid enum property: " + propertyName);
            }

            return parsed;
        }

        private static T OptionalEnum<T>(JsonElement element, string propertyName, T defaultValue)
            where T : struct
        {
            if (!element.TryGetProperty(propertyName, out JsonElement property) ||
                property.ValueKind == JsonValueKind.Null)
            {
                return defaultValue;
            }

            if (property.ValueKind != JsonValueKind.String)
            {
                throw new FormatException("Invalid enum property: " + propertyName);
            }

            if (!Enum.TryParse(property.GetString(), ignoreCase: true, out T parsed))
            {
                throw new FormatException("Invalid enum property: " + propertyName);
            }

            return parsed;
        }

        private static void EnsureArray(JsonElement element, string propertyName)
        {
            if (element.ValueKind != JsonValueKind.Array)
            {
                throw new FormatException("Invalid array property: " + propertyName);
            }
        }
    }
}
