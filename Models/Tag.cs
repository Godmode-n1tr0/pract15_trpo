using System;
using System.Collections.Generic;

namespace TRPO.ElectronicsStore.Models;

public partial class Tag
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
