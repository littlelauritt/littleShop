using MassTransit;
using littleShop.Shared.Events;
using littleShop.catalog.Data;

namespace littleShop.catalog.Consumers;

public class OrderCancelledConsumer(CatalogDbContext db) : IConsumer<OrderCancelledEvent>
{
    public async Task Consume(ConsumeContext<OrderCancelledEvent> context)
    {
        var message = context.Message;

        if (message.ItemsToRestore is null || message.ItemsToRestore.Count == 0) return;

        foreach (var item in message.ItemsToRestore)
        {
            var productId = item.Key;
            var qty = item.Value;

            var product = await db.Products.FindAsync(productId);

            if (product != null)
            {
                var oldStock = product.Stock;
                product.Stock += qty;

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.ResetColor();
            }
            else
            {
            }
        }

        await db.SaveChangesAsync();
    }
}