document.addEventListener('DOMContentLoaded', () => {
    // Elements
    const totalBalance = document.getElementById('total-balance');
    const totalExpenses = document.getElementById('total-expenses');
    const transactionList = document.getElementById('transaction-list');
    const pieSegment = document.getElementById('pie-segment');
    const expenseRatio = document.getElementById('expense-ratio');
    const form = document.getElementById('transaction-form');
    const addBtn = document.getElementById('add-expense-btn');
    const saveBtn = document.getElementById('save-expense');
    
    // Inputs
    const nameInput = document.getElementById('expense-name');
    const amountInput = document.getElementById('expense-amount');
    const categoryInput = document.getElementById('expense-category');

    let transactions = [];
    let monthlyIncome = 5000.00;

    function formatCurrency(num) {
        return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(num);
    }

    function calculateStats() {
        const totalExp = transactions.reduce((acc, curr) => acc + curr.amount, 0);
        const balance = monthlyIncome - totalExp;
        
        totalExpenses.innerText = formatCurrency(totalExp);
        document.getElementById('total-income').innerText = formatCurrency(monthlyIncome);
        totalBalance.innerText = formatCurrency(balance);

        const ratio = Math.min((totalExp / monthlyIncome) * 100, 100);
        pieSegment.style.strokeDasharray = `${ratio}, 100`;
        expenseRatio.innerText = `${Math.round(ratio)}%`;
        pieSegment.style.stroke = ratio > 80 ? '#ef4444' : '#3b82f6';
    }

    function drawTrendChart(data = [120, 80, 150, 100, 130, 90]) {
        const svg = document.getElementById('trend-line-chart');
        if (!svg) return;
        // Adjust points to fit in 400x150 with padding
        const points = data.map((val, i) => `${i * 65 + 40},${130 - (val / 200 * 100)}`).join(' ');
        svg.innerHTML = `
            <polyline points="${points}" fill="none" stroke="#3b82f6" stroke-width="4" stroke-linecap="round" stroke-linejoin="round" />
            ${data.map((val, i) => `<circle cx="${i * 65 + 40}" cy="${130 - (val / 200 * 100)}" r="5" fill="#3b82f6" stroke="white" stroke-width="2" />`).join('')}
        `;
    }

    function renderTransactions() {
        transactionList.innerHTML = '';
        // Sort by date or id descending
        [...transactions].reverse().forEach(t => {
            const item = document.createElement('div');
            item.className = 'transaction-item';
            
            const icons = { Food: '🍔', Tech: '💻', Study: '📚', Travel: '✈️' };
            
            item.innerHTML = `
                <div class="item-info">
                    <div class="icon-box">${icons[t.category] || '💰'}</div>
                    <div>
                        <p class="item-name">${t.name}</p>
                        <p class="item-cat">${t.date} • ${t.category}</p>
                    </div>
                </div>
                <div class="item-amount">-${formatCurrency(t.amount)}</div>
            `;
            transactionList.appendChild(item);
        });
    }

    addBtn.addEventListener('click', () => {
        form.classList.toggle('hidden');
    });

    saveBtn.addEventListener('click', () => {
        const name = nameInput.value.trim();
        const amount = parseFloat(amountInput.value);
        const category = categoryInput.value;

        if (!name || isNaN(amount)) return;

        const newTx = {
            id: Date.now(),
            name,
            amount,
            category,
            date: new Date().toISOString().split('T')[0]
        };

        transactions.push(newTx);
        calculateStats();
        renderTransactions();

        // Clear and hide
        nameInput.value = '';
        amountInput.value = '';
        form.classList.add('hidden');
    });

    function updateAnalytics() {
        const breakdown = {};
        transactions.forEach(t => {
            breakdown[t.category] = (breakdown[t.category] || 0) + t.amount;
        });

        const totalValue = transactions.reduce((a, b) => a + b.amount, 0);
        const container = document.getElementById('category-breakdown');
        const pieChart = document.getElementById('analytics-pie');
        
        const colors = { Food: '#ef4444', Tech: '#3b82f6', Study: '#10b981', Travel: '#a855f7' };
        
        // Render List & Pie
        let listHTML = '';
        let pieHTML = `<path class="circle-bg" d="M18 2.0845 a 15.9155 15.9155 0 0 1 0 31.831 a 15.9155 15.9155 0 0 1 0 -31.831" />`;
        let offset = 0;

        Object.entries(breakdown).forEach(([cat, amt]) => {
            const perc = totalValue > 0 ? (amt / totalValue * 100) : 0;
            const color = colors[cat] || '#94a3b8';
            
            listHTML += `
                <div class="category-stat" style="border-left-color: ${color}">
                    <span>${cat} (${perc.toFixed(1)}%)</span>
                    <strong>${formatCurrency(amt)}</strong>
                </div>
            `;

            // Draw Segment
            pieHTML += `
                <path class="circle" 
                    stroke="${color}"
                    stroke-dasharray="${perc}, 100"
                    stroke-dashoffset="-${offset}"
                    d="M18 2.0845 a 15.9155 15.9155 0 0 1 0 31.831 a 15.9155 15.9155 0 0 1 0 -31.831" 
                />
            `;
            offset += perc;
        });

        container.innerHTML = listHTML;
        pieChart.innerHTML = pieHTML;

        const advice = document.getElementById('smart-advice');
        if (totalValue > monthlyIncome * 0.8) advice.innerText = "Warning: High burn rate detected! (80%+). Consider reducing non-essential costs.";
        else advice.innerText = "Excellent financial management. Your category distribution is balanced.";
        
        drawTrendChart();
    }

    // Sidebar Tabs
    const navItems = document.querySelectorAll('.nav-item');
    const tabs = document.querySelectorAll('.tab-content');

    navItems.forEach((btn, index) => {
        btn.addEventListener('click', () => {
            navItems.forEach(b => b.classList.remove('active'));
            tabs.forEach(t => t.classList.remove('active'));
            btn.classList.add('active');
            tabs[index].classList.add('active');
            if (index === 1) updateAnalytics();
        });
    });

    // Theme Toggle
    const themeBtn = document.getElementById('theme-toggle');
    if (localStorage.getItem('fin-theme') === 'light') document.body.classList.add('light-mode');

    themeBtn.addEventListener('click', () => {
        document.body.classList.toggle('light-mode');
        localStorage.setItem('fin-theme', document.body.classList.contains('light-mode') ? 'light' : 'dark');
    });

    // New Logic
    document.getElementById('edit-budget').addEventListener('click', () => {
        const val = prompt("Set new Monthly Budget:", monthlyIncome);
        if (val && !isNaN(val)) {
            monthlyIncome = parseFloat(val);
            calculateStats();
        }
    });

    document.getElementById('load-demo-fin').addEventListener('click', () => {
        transactions = [
            { id: 1, name: 'AWS Cloud', amount: 120, category: 'Tech', date: '2026-04-01' },
            { id: 2, name: 'Groceries', amount: 85, category: 'Food', date: '2026-04-05' },
            { id: 3, name: 'New Desk', amount: 350, category: 'Study', date: '2026-04-10' },
            { id: 4, name: 'Airfare', amount: 650, category: 'Travel', date: '2026-04-15' }
        ];
        monthlyIncome = 4000;
        calculateStats();
        renderTransactions();
        drawTrendChart([110, 90, 140, 80, 120, 105]);
        alert("Demo Profile Loaded.");
    });

    // Initialize
    calculateStats();
    renderTransactions();
});
