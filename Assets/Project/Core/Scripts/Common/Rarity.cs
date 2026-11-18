namespace DungeonGathering.Inventory
{
    /// <summary>
    /// Bậc vật phẩm / hero, cao nhất trước (S=0 … D=4). Số giữ nguyên khi đổi F → D nên asset cũ không lệch. Order matches the colour squares in Basic Buttons 3 Fv.
    /// Lưu ý: Enum giá trị int nhỏ hơn = Rarity cao hơn (S mạnh nhất, D phổ thông).
    /// Khuyến khích dùng RarityExtensions để so sánh thay vì toán tử < hoặc >.
    /// </summary>
    public enum Rarity { S = 0, A = 1, B = 2, C = 3, D = 4 }

    public static class RarityExtensions
    {
        /// <summary>Trả về true nếu a có độ hiếm cao hơn b (ví dụ S cao hơn A, B cao hơn C).</summary>
        public static bool IsHigherThan(this Rarity a, Rarity b) => (int)a < (int)b;

        /// <summary>Trả về true nếu a có độ hiếm thấp hơn b (ví dụ D thấp hơn C).</summary>
        public static bool IsLowerThan(this Rarity a, Rarity b) => (int)a > (int)b;

        /// <summary>Trả về true nếu a đạt tối thiểu độ hiếm b (a bằng hoặc hiếm hơn b).</summary>
        public static bool IsAtLeast(this Rarity a, Rarity b) => (int)a <= (int)b;
    }
}
