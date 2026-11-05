namespace DungeonGathering.Inventory
{
    /// <summary>
    /// Item rarity, highest first (S=0 ... F=4). Order matches the colour squares in Basic Buttons 3 Fv.
    /// Lưu ý: Enum giá trị int nhỏ hơn = Rarity cao hơn (S mạnh nhất, F phổ thông).
    /// Khuyến khích dùng RarityExtensions để so sánh thay vì toán tử < hoặc >.
    /// </summary>
    public enum Rarity { S = 0, A = 1, B = 2, C = 3, F = 4 }

    public static class RarityExtensions
    {
        /// <summary>Trả về true nếu a có độ hiếm cao hơn b (ví dụ S cao hơn A, B cao hơn C).</summary>
        public static bool IsHigherThan(this Rarity a, Rarity b) => (int)a < (int)b;

        /// <summary>Trả về true nếu a có độ hiếm thấp hơn b (ví dụ F thấp hơn C).</summary>
        public static bool IsLowerThan(this Rarity a, Rarity b) => (int)a > (int)b;

        /// <summary>Trả về true nếu a đạt tối thiểu độ hiếm b (a bằng hoặc hiếm hơn b).</summary>
        public static bool IsAtLeast(this Rarity a, Rarity b) => (int)a <= (int)b;
    }
}
