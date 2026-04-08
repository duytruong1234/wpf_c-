using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using QuanLyKhoNguyenLieuPizza.Core.Interfaces;
using QuanLyKhoNguyenLieuPizza.Models;
using QuanLyKhoNguyenLieuPizza.ViewModels;
using Xunit;
using System.Collections.Generic;

namespace QuanLyKhoNguyenLieuPizza.Tests.ViewModels;

public class PizzaViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitializeCommands()
    {
        // Arrange
        var mockDb = new Mock<IDatabaseService>();
        
        // Act
        var viewModel = new PizzaViewModel(mockDb.Object);
        
        // Assert
        viewModel.LoadDataCommand.Should().NotBeNull();
        viewModel.OpenAddPopupCommand.Should().NotBeNull();
        viewModel.SaveCommand.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadDataCommand_ShouldFetchDataAndPopulateCollections()
    {
        // Arrange
        var mockDb = new Mock<IDatabaseService>();
        
        var mockPizzas = new List<Pizza> 
        { 
            new Pizza { PizzaID = 1, TenPizza = "Pizza Hải Sản", KichThuoc = "L", GiaBan = 150000, TrangThai = true },
            new Pizza { PizzaID = 2, TenPizza = "Pizza Xúc Xích", KichThuoc = "M", GiaBan = 100000, TrangThai = false }
        };
        var mockLoaiHangHoas = new List<LoaiHangHoa>
        {
            new LoaiHangHoa { LoaiHangHoaID = "LHH01", TenLoaiHangHoa = "Thức ăn" }
        };
        var mockDonVis = new List<DonViTinh>
        {
            new DonViTinh { DonViID = 1, TenDonVi = "Chiếc" }
        };

        mockDb.Setup(db => db.GetPizzasAsync())
              .ReturnsAsync(mockPizzas);
        mockDb.Setup(db => db.GetLoaiHangHoasAsync())
              .ReturnsAsync(mockLoaiHangHoas);
        mockDb.Setup(db => db.GetDonViTinhsAsync())
              .ReturnsAsync(mockDonVis);

        var viewModel = new PizzaViewModel(mockDb.Object);
        
        // Act
        // Invoke LoadDataCommand
        viewModel.LoadDataCommand.Execute(null);

        // Allow async command to finish
        await Task.Delay(100);
        
        // Assert
        viewModel.FilteredPizzas.Should().HaveCount(2);
        viewModel.FilteredPizzas[0].TenPizza.Should().Be("Pizza Hải Sản");
        viewModel.TotalPizza.Should().Be(2);
        viewModel.CountDangBan.Should().Be(1);
        viewModel.CountNgungBan.Should().Be(1);
    }
}
