﻿// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Test.BiproportionalApportionmentTests;

public static class ZhKantonratswahlTestData
{
    public static BiproportionalTestData Kantonratswahl2019()
    {
        var voteCountMatrix = new[]
        {
            new[] { 5662, 10910, 7360, 5902, 7844, 1733, 595, 2416, 234 },
            new[] { 33512, 70295, 23592, 28663, 40514, 9960, 5633, 19009, 579 },
            new[] { 2806, 13484, 3544, 6298, 7648, 605, 332, 6716, 132 },
            new[] { 19416, 47596, 23745, 23529, 28051, 4593, 3210, 13611, 527 },
            new[] { 10255, 18531, 19624, 11897, 12358, 3841, 1761, 3497, 254 },
            new[] { 34595, 48742, 19779, 22934, 25102, 7495, 7270, 8129, 2007 },
            new[] { 42481, 26964, 25055, 17993, 8943, 11792, 4696, 2059, 1912 },
            new[] { 19265, 12885, 10929, 10265, 7841, 2046, 6198, 645, 1666 },
            new[] { 90998, 62623, 80180, 44371, 38102, 27867, 17095, 5358, 5201 },
            new[] { 84129, 44284, 78979, 41532, 28743, 13575, 7707, 3387, 5799 },
            new[] { 65778, 27743, 29178, 21841, 26285, 12702, 12406, 3348, 13508 },
            new[] { 104683, 66288, 58214, 58620, 41076, 19030, 13213, 6346, 9538 },
            new[] { 29795, 13278, 13278, 10930, 9147, 2560, 9014, 1142, 4628 },
            new[] { 60891, 78239, 34703, 47208, 49152, 12248, 17964, 12915, 5837 },
            new[] { 34466, 11390, 13911, 13606, 9274, 2963, 7823, 638, 2685 },
            new[] { 12437, 4796, 5329, 2891, 3289, 546, 1500, 348, 915 },
            new[] { 140138, 69786, 70138, 53964, 35950, 15590, 22574, 5680, 16807 },
            new[] { 64859, 21420, 21372, 19054, 14529, 5866, 4442, 1387, 8512 },
        };

        var electionNumberOfMandates = new[]
        {
            5, 12, 5, 9, 6, 12, 11, 6, 15, 12, 11, 16, 7, 13, 7, 4, 18, 11,
        };

        var unionListNumberOfMandates = new[]
        {
            45, 35, 29, 23, 22, 8, 8, 6, 4,
        };

        var listNames = new[]
        {
            "SVP", "SP", "FDP", "GLP",
            "Grüne", "CVP", "EVL", "AL", "EDU",
        };

        return new BiproportionalTestData(voteCountMatrix, electionNumberOfMandates, unionListNumberOfMandates, listNames);
    }

    public static BiproportionalTestData Kantonratswahl2015()
    {
        var voteCountMatrix = new[]
        {
            new[] { 5254, 7936, 6627, 2594, 3558, 1398, 498, 1672, 247, 0 },
            new[] { 40938, 59704, 24457, 15292, 21153, 10345, 5471, 16831, 1260, 1811 },
            new[] { 3083, 10421, 3057, 3306, 5124, 728, 324, 5917, 185, 254 },
            new[] { 24108, 43212, 23290, 12614, 14986, 5605, 3157, 13162, 868, 1884 },
            new[] { 12107, 18251, 20260, 6166, 7934, 3421, 1681, 3571, 334, 579 },
            new[] { 42103, 43393, 18551, 12147, 12862, 10313, 5687, 6889, 2430, 3382 },
            new[] { 52052, 25020, 28696, 8038, 5283, 11961, 5487, 1792, 1620, 2288 },
            new[] { 21870, 12297, 12280, 5557, 4737, 2599, 5882, 691, 1510, 0 },
            new[] { 109374, 62568, 80566, 27859, 22022, 30240, 15687, 4659, 5684, 10657 },
            new[] { 101578, 47955, 93626, 25014, 16325, 14596, 7170, 3734, 8963, 6390 },
            new[] { 83273, 30668, 32729, 15830, 14655, 13017, 13598, 3009, 15419, 8358 },
            new[] { 127780, 67381, 59884, 34535, 21491, 17936, 11775, 4414, 9767, 21761 },
            new[] { 32872, 14694, 13295, 6425, 5325, 2662, 7570, 759, 4142, 2768 },
            new[] { 68076, 75188, 39016, 25746, 28613, 16907, 23650, 11802, 7089, 8784 },
            new[] { 37310, 11911, 12989, 8133, 4711, 2914, 6458, 777, 2847, 4181 },
            new[] { 13225, 4875, 5740, 1739, 2972, 597, 1007, 302, 1224, 1939 },
            new[] { 151323, 67966, 66897, 25543, 20355, 16196, 18532, 3608, 16968, 14239 },
            new[] { 72703, 23304, 22143, 10429, 8982, 5574, 4184, 875, 9461, 2739 },
        };

        var electionNumberOfMandates = new[]
        {
            4, 12, 5, 9, 6, 12, 11, 6, 15, 13, 12, 16, 7, 13, 7, 4, 17, 11,
        };

        var unionListNumberOfMandates = new[]
        {
            54, 36, 31, 14, 13, 9, 8, 5, 5, 5,
        };

        var listNames = new[]
        {
            "SVP", "SP", "FDP", "GLP",
            "Grüne", "CVP", "EVP", "AL", "EDU", "BDP",
        };

        return new BiproportionalTestData(voteCountMatrix, electionNumberOfMandates, unionListNumberOfMandates, listNames);
    }

    public static BiproportionalTestData Kantonratswahl2011()
    {
        var voteCountMatrix = new[]
        {
            new[] { 7356, 11528, 7327, 5752, 4404, 1863, 574, 0, 364, 939 },
            new[] { 43229, 62846, 16278, 30034, 21426, 10762, 5448, 3561, 1525, 9990 },
            new[] { 3503, 11620, 1851, 6287, 3806, 1131, 396, 0, 110, 3997 },
            new[] { 25336, 46638, 18416, 23212, 18495, 6068, 3428, 2537, 1059, 6537 },
            new[] { 13257, 18816, 15196, 12849, 9791, 3597, 1615, 1614, 388, 1929 },
            new[] { 45238, 46035, 13978, 18774, 15810, 10414, 5787, 3710, 2707, 3549 },
            new[] { 55351, 27477, 25552, 11641, 8798, 11970, 5835, 3036, 1770, 1838 },
            new[] { 22553, 11314, 9566, 7708, 8021, 2364, 5529, 3600, 1957, 311 },
            new[] { 114747, 69270, 66809, 34602, 37419, 30096, 17654, 14387, 6212, 1874 },
            new[] { 108013, 45805, 78678, 27687, 42106, 15133, 8284, 8997, 8385, 1438 },
            new[] { 87214, 33077, 23732, 21943, 22578, 13890, 13391, 10313, 16079, 1455 },
            new[] { 131223, 72078, 44655, 33690, 54143, 17558, 10546, 28127, 10376, 3181 },
            new[] { 35166, 14327, 9793, 10527, 9055, 2995, 6187, 4820, 4471, 372 },
            new[] { 67083, 67232, 33605, 45258, 31774, 18625, 16519, 8143, 7136, 8233 },
            new[] { 38482, 13294, 10734, 7994, 9847, 3768, 6354, 4292, 3228, 330 },
            new[] { 14904, 5046, 4442, 3817, 2643, 778, 998, 2527, 1226, 163 },
            new[] { 155561, 71493, 51130, 32137, 39438, 17222, 17081, 21598, 15889, 3016 },
            new[] { 66891, 22947, 15321, 12290, 14946, 6217, 3398, 3981, 7583, 546 },
        };

        var electionNumberOfMandates = new[]
        {
            5, 12, 5, 9, 6, 12, 11, 6, 15, 13, 12, 16, 7, 13, 7, 4, 17, 10,
        };

        var unionListNumberOfMandates = new[]
        {
            54, 35, 23, 19, 19, 9, 7, 6, 5, 3,
        };

        var listNames = new[]
        {
            "SVP", "SP", "FDP", "Gruene",
            "GLP", "CVP", "EVP", "BDP", "EDU", "AL",
        };

        return new BiproportionalTestData(voteCountMatrix, electionNumberOfMandates, unionListNumberOfMandates, listNames);
    }
}
