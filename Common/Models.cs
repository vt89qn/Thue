using System.Text.Json;

namespace Thue.Common;

public class LoaiHoaDon()
{
	public string title { get; set; }
	public LoaiHoaDonValue value { get; set; }

	public static List<LoaiHoaDon> List = JsonSerializer.Deserialize<List<LoaiHoaDon>>("[{\"value\":{\"khmshdon\":1,\"hdon\":\"01\"},\"title\":\"Hóa đơn điện tử giá trị gia tăng\"},{\"value\":{\"khmshdon\":2,\"hdon\":\"02\"},\"title\":\"Hóa đơn bán hàng\"},{\"value\":{\"khmshdon\":3,\"hdon\":\"03\"},\"title\":\"Hóa đơn bán tài sản công\"},{\"value\":{\"khmshdon\":4,\"hdon\":\"04\"},\"title\":\"Hóa đơn bán hàng dự trữ quốc gia\"},{\"value\":{\"khmshdon\":5,\"hdon\":\"05\"},\"title\":\"Hóa đơn khác\"},{\"value\":{\"khmshdon\":6,\"hdon\":\"06_01\"},\"title\":\"Phiếu xuất kho kiêm vận chuyển nội bộ\"},{\"value\":{\"khmshdon\":6,\"hdon\":\"06_02\"},\"title\":\"Phiếu xuất kho gửi bán hàng đại lý\"},{\"value\":{\"khmshdon\":7,\"hdon\":\"07\"},\"title\":\"Hoá đơn thương mại\"},{\"value\":{\"khmshdon\":8,\"hdon\":\"08\"},\"title\":\"Hóa đơn giá trị gia tăng tích hợp biên lai thu thuế, phí, lệ phí\"},{\"value\":{\"khmshdon\":9,\"hdon\":\"09\"},\"title\":\"Hóa đơn bán hàng tích hợp biên lai thu thuế, phí, lệ phí\"}]");
}
public class LoaiHoaDonValue
{
	public int khmshdon { get; set; }
	public string hdon { get; set; }
}