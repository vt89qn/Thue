using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections;

namespace Thue.Common;

public interface IGridSetting
{
	static abstract string Setting { get; }
}
public abstract class GridDropdown
{
	public string Text { get; set; }

	public abstract IList GetSource();

	public override string ToString()
	{
		return Text;
	}
}
public class GridDropdown<T> : GridDropdown
{
	public T Value { get; set; }

	public override IList GetSource()
	{
		return new List<GridDropdown<T>>();
	}
}
public partial class TraCuuHoaDonGridModel : ObservableObject, IGridSetting
{
	public int RowIdx { get; set; }
	public int ColIdx { get; set; }
	public int Stt { get; set; }
	public string MstNguoiBan { get; set; }
	public string KyHieuHoaDon { get; set; }
	public string SoHoaDon { get; set; }
	public string TongTienThanhToan { get; set; }
	public string LoaiHoaDon { get; set; }
	[ObservableProperty] public partial string Result { get; set; }
	public string ScreenShot { get; set; }
	public bool IsSuccess { get; set; } = false;
	public bool IsPendingSave { get; set; } = false;
	public DateTime UpdateAt { get; set; } = DateTime.MinValue;

	[ObservableProperty] public partial string Info { get; set; }
	public static string Setting => $"{nameof(Stt)},Stt,40|{nameof(MstNguoiBan)},MST,80|{nameof(KyHieuHoaDon)},Ký hiệu HĐ,80|{nameof(SoHoaDon)},Số HĐ,60|{nameof(TongTienThanhToan)},Tổng tiền TT,100|{nameof(LoaiHoaDon)},Loại HĐ,120|{nameof(Result)},Kết quả,400|{nameof(Info)},Thông tin,-1";
}
public partial class TrangThaiMstGridModel : ObservableObject, IGridSetting
{
	public int RowIdx { get; set; }
	public int ColIdx { get; set; }
	public int Stt { get; set; }
	public string Mst { get; set; }
	[ObservableProperty] public partial string Ten { get; set; }
	[ObservableProperty] public partial string DiaChi { get; set; }
	[ObservableProperty] public partial string CoQuanThue { get; set; }
	[ObservableProperty] public partial string TinhTrang { get; set; }
	public string ScreenShot { get; set; }
	public bool IsSuccess { get; set; } = false;
	public bool IsPendingSave { get; set; } = false;
	public DateTime UpdateAt { get; set; } = DateTime.MinValue;

	[ObservableProperty] public partial string Info { get; set; }
	public static string Setting => $"{nameof(Stt)},Stt,40|{nameof(Mst)},MST,80|{nameof(Ten)},Tên người nộp thuế,150|{nameof(DiaChi)},Địa chỉ,100|{nameof(CoQuanThue)},Cơ quan thuế,100|{nameof(TinhTrang)},Tình trạng,100|{nameof(Info)},Thông tin,-1";
}
public partial class CuongCheThueGridModel : ObservableObject, IGridSetting
{
	public int RowIdx { get; set; }
	public int ColIdx { get; set; }
	public int Stt { get; set; }
	public string Mst { get; set; }
	[ObservableProperty] public partial string Ten { get; set; }
	[ObservableProperty] public partial string SoQd { get; set; }
	[ObservableProperty] public partial string NgayQd { get; set; }
	[ObservableProperty] public partial string Cqt { get; set; }
	[ObservableProperty] public partial string SoTien { get; set; }
	[ObservableProperty] public partial string BienPhap { get; set; }
	[ObservableProperty] public partial string ThongBao { get; set; }
	[ObservableProperty] public partial string HieuLucTu { get; set; }
	[ObservableProperty] public partial string HieuLucDen { get; set; }
	[ObservableProperty] public partial string ThoiGian { get; set; }
	[ObservableProperty] public partial string TrangThai { get; set; }

	public string ScreenShot { get; set; }
	public bool NeedScreenShot { get; set; } = false;
	public bool IsSuccess { get; set; } = false;
	public bool IsPendingSave { get; set; } = false;
	public DateTime UpdateAt { get; set; } = DateTime.MinValue;

	[ObservableProperty] public partial string Info { get; set; }
	public static string Setting => $"{nameof(Stt)},Stt,40|{nameof(Mst)},MST,80|{nameof(Ten)},Tên NTT,100|{nameof(SoQd)},Số QĐ,50|{nameof(NgayQd)},Ngày,50|{nameof(Cqt)},CQT,50|{nameof(SoTien)},Số tiền,50|{nameof(BienPhap)},Biện pháp,70|{nameof(ThongBao)},Thông báo,80|{nameof(HieuLucTu)},H lực từ,70|{nameof(HieuLucDen)},H lực đến,70|{nameof(ThoiGian)},Thời gian,70|{nameof(TrangThai)},Trạng thái,70|{nameof(Info)},Thông tin,-1";
}
