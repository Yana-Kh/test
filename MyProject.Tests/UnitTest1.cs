using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;

namespace YourProject.Tests
{
    public class OrderServiceTests
    {
        [Fact]
        public async Task CalculateTotalAsync_NormalOrder_And_CallsVatOnce()
        {
            // Установка значений
            var order = new Order("order-1", 
                new List<OrderItem>
                {
                    new("sku1", 2, 10m),
                    new("sku2", 1, 5m)
                }, 
                Shipping: 7m, 
                DiscountPercent: 10m);

            var repo = new Mock<IOrderRepository>();
            repo.Setup(r => r.GetAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

            var expectedNet = 28.8m;
            var expectedVat = 5.76m;

            var tax = new Mock<ITaxService>();
            tax.Setup(t => t.CalculateVat(expectedNet)).Returns(expectedVat);

            var sut = new OrderService(repo.Object, tax.Object);

            // Вызов метода
            var total = await sut.CalculateTotalAsync(order.Id);

            // Проверка
            total.Should().Be(expectedNet + expectedVat); 
            tax.Verify(t => t.CalculateVat(expectedNet), Times.Once);
        }

        [Fact]
        public async Task CalculateTotalAsync_ZeroDiscount()
        {
            // Установка значений
            var order = new Order("order-2", 
                new List<OrderItem> 
                { 
                    new("sku1", 1, 30m) 
                }, 
                Shipping: 10m, 
                DiscountPercent: 0m);

            var repo = new Mock<IOrderRepository>();
            repo.Setup(r => r.GetAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

            var expectedNet = 40m;
            var vat = 8m;
            var tax = new Mock<ITaxService>();
            tax.Setup(t => t.CalculateVat(expectedNet)).Returns(vat);

            var sut = new OrderService(repo.Object, tax.Object);

            // Вызов метода
            var total = await sut.CalculateTotalAsync(order.Id);

            // Проверка
            total.Should().Be(expectedNet + vat);
            tax.Verify(t => t.CalculateVat(expectedNet), Times.Once);
        }

        [Fact]
        public async Task CalculateTotalAsync_FullDiscount()
        {
            // Установка значений
            var order = new Order("order-3", 
                new List<OrderItem> 
                { 
                    new("sku1", 1, 50m) 
                }, 
                Shipping: 10m, 
                DiscountPercent: 100m);

            var repo = new Mock<IOrderRepository>();
            repo.Setup(r => r.GetAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

            var expectedNet = 0m;
            var vat = 0m;
            var tax = new Mock<ITaxService>();
            tax.Setup(t => t.CalculateVat(expectedNet)).Returns(vat);

            var sut = new OrderService(repo.Object, tax.Object);
            
            // Вызов метода
            var total = await sut.CalculateTotalAsync(order.Id);

            // Проверка
            total.Should().Be(0m);
            tax.Verify(t => t.CalculateVat(expectedNet), Times.Once);
        }

        [Fact]
        public async Task CalculateTotalAsync_NoShipping()
        {
            // Установка значений
            var order = new Order("order-4", 
                new List<OrderItem> 
                { 
                    new("sku1", 1, 100m) 
                }, 
                Shipping: 0m, 
                DiscountPercent: 20m);

            var repo = new Mock<IOrderRepository>();
            repo.Setup(r => r.GetAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

            var expectedNet = 80m;
            var vat = 16m;
            var tax = new Mock<ITaxService>();
            tax.Setup(t => t.CalculateVat(expectedNet)).Returns(vat);

            var sut = new OrderService(repo.Object, tax.Object);

            // Вызов метода
            var total = await sut.CalculateTotalAsync(order.Id);

            // Проверка
            total.Should().Be(expectedNet + vat);
            tax.Verify(t => t.CalculateVat(expectedNet), Times.Once);
        }
        
        [Fact]
        public async Task CalculateTotalAsync_InvalidOrderId()
        {
            // Установка значений
            var repo = new Mock<IOrderRepository>();
            repo.Setup(r => r.GetAsync("invalid-id", It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);

            var tax = new Mock<ITaxService>();
            var sut = new OrderService(repo.Object, tax.Object);


            // Вызов метода
            Func<Task> act = async () => await sut.CalculateTotalAsync("invalid-id");

            // Проверка
            await act.Should().ThrowAsync<InvalidOperationException>();
            tax.Verify(t => t.CalculateVat(It.IsAny<decimal>()), Times.Never);
        }
    }
}