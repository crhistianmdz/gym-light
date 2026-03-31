using GymFlow.Application.Common;
using GymFlow.Application.DTOs;
using GymFlow.Application.UseCases;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Interfaces;
using Moq;
using Xunit;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GymFlow.Tests.Application.UseCases;

public class CreateSaleUseCaseTests
{
    private readonly Mock<IProductRepository> _productRepoMock;
    private readonly Mock<ISaleRepository> _saleRepoMock;
    private readonly CreateSaleUseCase _useCase;

    public CreateSaleUseCaseTests()
    {
        _productRepoMock = new Mock<IProductRepository>();
        _saleRepoMock = new Mock<ISaleRepository>();
        _useCase = new CreateSaleUseCase(_productRepoMock.Object, _saleRepoMock.Object);
    }

    private static Product CreateProduct(int stock = 10, decimal price = 100m)
        => Product.Create("Proteína", price, stock);

    private static CreateSaleRequestDto CreateRequest(params (Guid productId, int qty)[] lines)
        => new CreateSaleRequestDto(
            ClientGuid: Guid.NewGuid(),
            Lines: lines.Select(l => new SaleLineRequestDto(l.productId, l.qty)).ToList(),
            PerformedByUserId: Guid.NewGuid()
        );

    [Fact]
    public async Task ExecuteAsync_RequestWithoutLines_ReturnsValidationError()
    {
        // Arrange
        var request = CreateRequest();

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("al menos un producto");
        _productRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _saleRepoMock.Verify(r => r.AddAsync(It.IsAny<Sale>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ClientGuidAlreadyProcessed_ReturnsExistingSale()
    {
        // Arrange
        var existingSale = new Mock<Sale>();
        existingSale.Setup(s => s.ToDto()).Returns(new SaleDto());
        _saleRepoMock.Setup(r => r.GetByClientGuidAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(existingSale.Object);
        var request = CreateRequest((Guid.NewGuid(), 1));

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        _productRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _saleRepoMock.Verify(r => r.AddAsync(It.IsAny<Sale>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ProductNotFound_ReturnsNotFound()
    {
        // Arrange
        _productRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);
        var request = CreateRequest((Guid.NewGuid(), 1));

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("no encontrado");
        _saleRepoMock.Verify(r => r.AddAsync(It.IsAny<Sale>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_InsufficientStock_ReturnsValidationError()
    {
        // Arrange
        var product = CreateProduct(stock: 2);
        _productRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(product);
        var request = CreateRequest((product.Id, 5));

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Stock insuficiente");
        _saleRepoMock.Verify(r => r.AddAsync(It.IsAny<Sale>(), It.IsAny<CancellationToken>()), Times.Never);
        _productRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_SuccessfulSale_ReturnsSuccess()
    {
        // Arrange
        var product1 = CreateProduct(stock: 10, price: 100m);
        var product2 = CreateProduct(stock: 5, price: 200m);

        _productRepoMock.Setup(r => r.GetByIdAsync(product1.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product1);
        _productRepoMock.Setup(r => r.GetByIdAsync(product2.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product2);
        _saleRepoMock.Setup(r => r.GetByClientGuidAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Sale?)null);

        var request = CreateRequest((product1.Id, 2), (product2.Id, 1));

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Total.Should().Be(400);
        result.Value.Lines.Count.Should().Be(2);

        _productRepoMock.Verify(r => r.UpdateAsync(product1, It.IsAny<CancellationToken>()), Times.Once);
        _productRepoMock.Verify(r => r.UpdateAsync(product2, It.IsAny<CancellationToken>()), Times.Once);
        _saleRepoMock.Verify(r => r.AddAsync(It.IsAny<Sale>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}