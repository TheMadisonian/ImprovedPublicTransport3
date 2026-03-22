namespace AutoLineColor.Coloring
{
    internal static class KnownColorSet
    {
        public static IColorSetLoader Any { get; } = new SimpleColorSetLoader(
            "Any",
            "all.txt",
            "#f50302, #d60404, #990606, #c71818, #f52020, #991818, #d62f2f, #f53838, #991d02," +
            " #b82606, #d6340f, #993018, #f54f2a, #c74528, #c74900, #e55e10, #a8460d, #c75b1c," +
            " #994b1d, #e5712e, #b85e2a, #e58005, #b86909, #995c12, #e5922c, #c7812c, #997102," +
            " #d6a00b, #f5ba18, #b88c14, #e5b737, #997a25, #e5d410, #a89c19, #c7b922, #98a802," +
            " #b3c704, #d0e610, #8c991c, #8fc702, #7da811, #b0e627, #789925, #7de605, #6dc706," +
            " #5ea808, #629923, #3e990c, #69f51d, #37b81d, #54e637, #07f50b, #06c709, #29d64b," +
            " #22a83d, #38f55e, #0ec755, #29e671, #20a854, #069959, #11f592, #1ad685, #1fb876," +
            " #00f5b8, #02a87f, #2ee6b8, #22997b, #30c7a1, #03f5e5, #04d6c8, #28a8a0, #1ddcf5," +
            " #2ab5c7, #2599a8, #008bc7, #0aaef5, #147ca8, #29ade6, #0268c7, #0359a8, #107ee6," +
            " #155999, #2480d6, #216fb8, #3699f5, #0b45b8, #0f51d6, #0f3d99, #2268f5, #2058c7," +
            " #336ee6, #234a99, #2c5bb8, #0620b8, #1128a8, #2741d6, #2138b8, #1f3199, #3855f5," +
            " #0700d6, #0600b8, #0b02f5, #140f99, #3b35e6, #4619e6, #3e18c7, #4f2ec7, #3e2599," +
            " #4402a8, #7622f5, #712dd6, #5a25a8, #590099, #820dd6, #700db8, #a12cf5, #8830c7," +
            " #692599, #a502d6, #760399, #c014f5, #900fb8, #bc35e6, #8a28a8, #be0ec7, #ea14f5," +
            " #a223a8, #b80ba1, #e617ca, #991587, #b8027e, #f505a9, #990c6c, #d62da1, #a80054," +
            " #c70666, #e50e7a, #c72e7a, #f53b98, #99255f, #c7003f, #f5034f, #a80236, #99123d," +
            " #d61c57, #b81d4e, #f52f6d, #b8041c, #e5203a, #c72238, #a81e31, #e5374e");

        public static IColorSetLoader Blue { get; } = new SimpleColorSetLoader(
            "Blue",
            "blues.txt",
            "#16f5f1, #1ed6d3, #18a8a6, #00a6c7, #30c7e6, #2893a8, #0095e6, #056fa8, #1288c7," +
            " #2faff5, #226f99, #2c87b8, #034999, #0667d6, #075ab8, #167ef5, #1459a8, #1c73d6," +
            " #2c74c7, #3587e6, #255b99, #0a4cf5, #063099, #0b3cb8, #2761f5, #2255d6, #1f4ab8," +
            " #1a3e99, #3768e6, #031bf5, #0217d6, #0214b8, #021199, #1628c7, #1423a8, #2236e6," +
            " #2d3ed6");

        public static IColorSetLoader Green { get; } = new SimpleColorSetLoader(
            "Green",
            "green.txt",
            "#6af500, #54b807, #7ad633, #8df53d, #467a1f, #66a832, #629939, #527a33, #87c756," +
            " #b6f587, #75995a, #a9d687, #97b87f, #738a62, #349912, #54d629, #3d8a24, #56b835," +
            " #83f55d, #87e667, #70b858, #629950, #8ed676, #7fb86c, #567a49, #b8f5a4, #087a00," +
            " #13a808, #2f7a2a, #45a83e, #427a3e, #a0f59a, #9cd698, #0be625, #3ed650, #4ef562," +
            " #6bd678, #7ff58d, #65b86f, #57995f, #79b880, #547a59, #06b83b, #048a2c, #2ab855," +
            " #28994a, #49e678, #2f8a4a, #367a4a, #74d691, #89f5a9, #5d996f, #93e6ac, #7fb890," +
            " #98d6ab");

        public static IColorSetLoader Orange { get; } = new SimpleColorSetLoader(
            "Orange",
            "orange.txt",
            "#a80000, #c70202, #e51515, #991212, #c71c1c, #e52c2c, #b82727, #992525, #f52f02," +
            " #b82504, #991f03, #e5411c, #99311a, #b84025, #f55d3b, #d65133, #b84402, #f55e07," +
            " #d65911, #99400c, #c7632a, #a85525, #f57d38, #e58005, #c76f04, #a86718, #f5992a," +
            " #c7812c, #996423, #997000, #b88806, #f5bc20, #d6a51e, #a88628, #d6c400, #a89c16," +
            " #f5e322, #c7ba2c");

        public static IColorSetLoader Bright { get; } = new SimpleColorSetLoader(
            "Bright",
            "bright.txt",
            "#00ee20, #4900ff, #ffc000, #00e4ff, #ff00b4, #60ff00, #0014ff, #ff4500, #00ffc9," +
            " #ea00ff, #a5ff00, #006eff, #ff0300, #00ff8a, #9900ff, #fff700, #00acff, #ff005f," +
            " #00a816, #3300b4, #b48700, #00a1b4, #b4007f, #43b400, #000eb4, #b43000, #00b48e," +
            " #a500b4, #74b400, #004db4, #b40200, #00b461, #6c00b4, #b4ae00, #0079b4, #b40043");

        public static IColorSetLoader Pale { get; } = new SimpleColorSetLoader(
            "Pale",
            "pale.txt",
            "#bccaff, #ffbcbc, #bcffd2, #d6bcff, #fffbbc, #bcddff, #ffbcc6, #bcf7bd, #c2bcff," +
            " #ffe4bc, #bcf3ff, #ffbce0, #c7ffbc, #bcbcff, #ffc1bc, #bcffe8, #f5bcff, #daffbc," +
            " #a6baff, #ffa6a6, #a6ffc4, #cba6ff, #fffaa6, #a6d3ff, #ffa6b5, #a6f5a8, #afa6ff," +
            " #ffdda6, #a6efff, #ffa6d7, #b6ffa6, #a6a7ff, #ffaea6, #a6ffe1, #f3a6ff, #d0ffa6," +
            " #7f9eff, #ff7f7f, #7fffae, #b77fff, #fff97f, #7fc3ff, #ff7f97, #7ff282, #8d7fff," +
            " #ffd17f, #7feaff, #ff7fc9, #97ff7f, #7f80ff, #ff8c7f, #7fffd7, #ef7fff, #bfff7f");

        public static IColorSetLoader Dark { get; } = new SimpleColorSetLoader(
            "Dark",
            "dark.txt",
            "#7f2200, #007f64, #75007f, #527f00, #00377f, #7f0100, #007f45, #4c007f, #7f7b00," +
            " #00567f, #7f002f, #007710, #24007f, #7f6000, #00727f, #7f005a, #307f00, #000a7f");

        public static IColorSetLoader Named { get; } = new NamedColorSetLoader(
            "Named",
            "named.txt",
            "#00A550 Green, #0050A7 Blue, #EA1C23 Red, #FF6319 Orange, #FCCC0A Yellow, #27C0E8 Sky," +
            " #9A9C9D Silver, #63319F Purple, #62361B Brown, #254A00 Forest, #FFB2B9 Pink," +
            " #891E40 Plum, #68862D Olive, #F86758 Salmon, #646569 Gray, #80BD4A Mint");
    }
}
