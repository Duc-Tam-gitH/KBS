// src/pages/customer/Shop.jsx
import React from 'react';
import '../page.css';

const Shop = () => {
  // Dữ liệu mẫu (Mock data)
  const products = [
    { id: 1, name: 'Keychron Q1 Pro', price: '4,500,000đ', switch: 'Brown Switch' },
    { id: 2, name: 'Akko 3098B', price: '1,890,000đ', switch: 'Blue Switch' },
    { id: 3, name: 'NuPhy Halo75', price: '3,200,000đ', switch: 'Red Switch' },
    { id: 4, name: 'Vortex Race 3', price: '3,500,000đ', switch: 'Clear Switch' },
  ];

  return (
    <div className="page-container">
      <div className="shop-header">
        <h1>Tất cả sản phẩm</h1>
        <span style={{ color: 'var(--text-muted)' }}>{products.length} kết quả</span>
      </div>

      <div className="product-grid">
        {products.map((product) => (
          <div className="product-card" key={product.id}>
            <div className="product-img">[Ảnh {product.name}]</div>
            <div className="product-info">
              <div>
                <div className="product-name">{product.name}</div>
                <div style={{ fontSize: '13px', color: 'var(--text-muted)', marginTop: '4px' }}>
                  {product.switch}
                </div>
              </div>
              <div className="product-price">{product.price}</div>
            </div>
            <button className="btn-minimal" style={{ width: '100%' }}>Thêm vào giỏ</button>
          </div>
        ))}
      </div>
    </div>
  );
};

export default Shop;