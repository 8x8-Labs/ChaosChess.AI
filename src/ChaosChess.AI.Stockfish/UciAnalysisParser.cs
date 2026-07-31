using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Stockfish
{
    public static class UciAnalysisParser
    {
        public static bool TryParseInfoLine(string? line, out UciAnalysisInfo? info)
        {
            info = null;

            string[] tokens = SplitTokens(line);

            if (tokens.Length == 0 || tokens[0] != "info")
            {
                return false;
            }

            int depth = 0;
            int multipv = 1;
            int? scoreCentipawns = null;
            int? mateIn = null;
            UciScoreBound bound = UciScoreBound.Exact;
            var principalVariation = new List<string>();

            for (int i = 1; i < tokens.Length; i++)
            {
                string token = tokens[i];

                if (token == "depth" && TryReadInt(tokens, i + 1, out int parsedDepth))
                {
                    depth = parsedDepth;
                    i++;
                }
                else if (token == "multipv" && TryReadInt(tokens, i + 1, out int parsedMultipv))
                {
                    multipv = parsedMultipv;
                    i++;
                }
                else if (token == "score" && i + 2 < tokens.Length)
                {
                    string scoreKind = tokens[i + 1];

                    if (!TryReadInt(tokens, i + 2, out int scoreValue))
                    {
                        return false;
                    }

                    if (scoreKind == "cp")
                    {
                        scoreCentipawns = scoreValue;
                        mateIn = null;
                    }
                    else if (scoreKind == "mate")
                    {
                        mateIn = scoreValue;
                        scoreCentipawns = null;
                    }
                    else
                    {
                        return false;
                    }

                    i += 2;

                    if (i + 1 < tokens.Length)
                    {
                        if (tokens[i + 1] == "lowerbound")
                        {
                            bound = UciScoreBound.Lower;
                            i++;
                        }
                        else if (tokens[i + 1] == "upperbound")
                        {
                            bound = UciScoreBound.Upper;
                            i++;
                        }
                    }
                }
                else if (token == "pv")
                {
                    for (int pvIndex = i + 1; pvIndex < tokens.Length; pvIndex++)
                    {
                        principalVariation.Add(tokens[pvIndex]);
                    }

                    break;
                }
            }

            if (multipv <= 0 || scoreCentipawns.HasValue == mateIn.HasValue)
            {
                return false;
            }

            info = new UciAnalysisInfo(
                depth,
                multipv,
                scoreCentipawns,
                mateIn,
                bound,
                principalVariation);
            return true;
        }

        public static bool TryParseBestMoveLine(string? line, out UciBestMove? bestMove)
        {
            bestMove = null;

            string[] tokens = SplitTokens(line);

            if (tokens.Length < 2 || tokens[0] != "bestmove")
            {
                return false;
            }

            string? move = tokens[1] == "none" ? null : tokens[1];
            string? ponderMove = null;

            if (tokens.Length >= 4 && tokens[2] == "ponder" && tokens[3] != "none")
            {
                ponderMove = tokens[3];
            }

            bestMove = new UciBestMove(move, ponderMove);
            return true;
        }

        public static IReadOnlyList<MoveCandidate> ToMoveCandidates(
            IEnumerable<UciAnalysisInfo> infos,
            int variationCount)
        {
            if (infos == null)
            {
                throw new ArgumentNullException(nameof(infos));
            }

            if (variationCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(variationCount), variationCount, "Variation count must be positive.");
            }

            var bestByMultipv = new Dictionary<int, UciAnalysisInfo>();

            foreach (UciAnalysisInfo info in infos)
            {
                if (info == null)
                {
                    throw new ArgumentException("Analysis info collection cannot contain null.", nameof(infos));
                }

                if (!bestByMultipv.TryGetValue(info.Multipv, out UciAnalysisInfo? existing) ||
                    info.Depth >= existing.Depth)
                {
                    bestByMultipv[info.Multipv] = info;
                }
            }

            var multipvIndexes = new List<int>(bestByMultipv.Keys);
            multipvIndexes.Sort();

            var candidates = new List<MoveCandidate>();
            var seenMoves = new HashSet<string>(StringComparer.Ordinal);

            foreach (int multipv in multipvIndexes)
            {
                if (candidates.Count >= variationCount)
                {
                    break;
                }

                UciAnalysisInfo info = bestByMultipv[multipv];

                if (info.PrincipalVariation.Count == 0)
                {
                    continue;
                }

                string move = info.PrincipalVariation[0];

                if (!IsValidUciMove(move) || !seenMoves.Add(move))
                {
                    continue;
                }

                candidates.Add(new MoveCandidate(
                    move,
                    info.ScoreCentipawns,
                    info.MateIn));
            }

            return candidates;
        }

        private static string[] SplitTokens(string? line)
        {
            return string.IsNullOrWhiteSpace(line)
                ? Array.Empty<string>()
                : line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static bool TryReadInt(string[] tokens, int index, out int value)
        {
            value = 0;
            return index < tokens.Length && int.TryParse(tokens[index], out value);
        }

        private static bool IsValidUciMove(string move)
        {
            if (move.Length != 4 && move.Length != 5)
            {
                return false;
            }

            if (!IsSquare(move[0], move[1]) || !IsSquare(move[2], move[3]))
            {
                return false;
            }

            return move.Length == 4 || IsAsciiLetter(move[4]);
        }

        private static bool IsSquare(char file, char rank)
        {
            return file >= 'a' && file <= 'h' && rank >= '1' && rank <= '8';
        }

        private static bool IsAsciiLetter(char value)
        {
            return (value >= 'a' && value <= 'z') ||
                (value >= 'A' && value <= 'Z');
        }
    }
}
