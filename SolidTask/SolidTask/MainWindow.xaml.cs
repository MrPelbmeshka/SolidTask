using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SolidTask
{
    public partial class MainWindow : Window
    {
        private List<Employee> employees;

        public MainWindow()
        {
            InitializeComponent();
            LoadEmployees();
        }

        private void LoadEmployees()
        {
            using var db = new AppDbContext();
            employees = db.Employees.ToList();
            employeeList.ItemsSource = employees;
        }

        private void AddEmployee_Click(object sender, RoutedEventArgs e)
        {
            var employee = new Employee
            {
                Name = txtName.Text,
                PaymentType = cmbPaymentType.Text,
                EmployeeNumber = txtEmployeeNumber.Text
            };

            using var db = new AppDbContext();
            db.Employees.Add(employee);
            db.SaveChanges();
            LoadEmployees();
        }

        private void employeeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = employeeList.SelectedItem as Employee;
            if (selected != null)
            {
                selectedType.Text = selected.PaymentType;
                UpdateVisibility(selected.PaymentType);
            }
        }


        private void UpdateVisibility(string type)
        {
            hourlyRatePanel.Visibility = Visibility.Collapsed;
            normPanel.Visibility = Visibility.Collapsed;
            unitsPanel.Visibility = Visibility.Collapsed;
            bonusPanel.Visibility = Visibility.Collapsed;
            percentPanel.Visibility = Visibility.Collapsed;
            baseSalaryPanel.Visibility = Visibility.Collapsed;
            

            switch (type)
            {
                case "Простая":
                    hourlyRatePanel.Visibility = Visibility.Visible;
                    normPanel.Visibility = Visibility.Visible;
                    unitsPanel.Visibility = Visibility.Visible;

                    break;
                case "Сдельно-премиальная":
                    hourlyRatePanel.Visibility = Visibility.Visible;
                    normPanel.Visibility = Visibility.Visible;
                    unitsPanel.Visibility = Visibility.Visible;
                    bonusPanel.Visibility = Visibility.Visible;
                    break;
                case "Сдельно-прогрессивная":
                    unitsPanel.Visibility = Visibility.Visible;
                    break;
                case "Косвенно-сдельная":
                    percentPanel.Visibility = Visibility.Visible;
                    baseSalaryPanel.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void CalculateSalary_Click(object sender, RoutedEventArgs e)
        {
            var selected = employeeList.SelectedItem as Employee;
            if (selected == null)
            {
                MessageBox.Show("Выберите сотрудника");
                return;
            }

            // Принцип открытости/закрытости (OCP): При добавлении новых типов оплаты, не нужно менять существующие классы.
            IPaymentCalculator calculator = selected.PaymentType switch
            {
                "Простая" => new SimplePieceworkPayment(decimal.Parse(txtHourlyRate.Text), decimal.Parse(txtNorm.Text), int.Parse(txtUnits.Text)),
                "Сдельно-премиальная" => new PremiumPieceworkPayment(decimal.Parse(txtHourlyRate.Text), decimal.Parse(txtNorm.Text), int.Parse(txtUnits.Text), decimal.Parse(txtBonus.Text)),
                "Сдельно-прогрессивная" => new ProgressivePieceworkPayment(int.Parse(txtUnits.Text)),
                "Косвенно-сдельная" => new IndirectPieceworkPayment(decimal.Parse(txtBaseSalary.Text), decimal.Parse(txtPercent.Text)),
                _ => null
            };

            if (calculator != null)
            {
                var salary = calculator.CalculateSalary();
                txtResult.Text = $"Зарплата: {salary} руб.";
            }
        }
    }

    // Принцип инверсии зависимостей (DIP): AppDbContext и другие классы не зависят от конкретных реализаций IPaymentCalculator,
    // а только от абстракций. Весь код может использовать разные классы с расчетом зарплаты, без привязки к реализации.
    public class AppDbContext : DbContext
    {
        public DbSet<Employee> Employees { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=TaskSolid;Integrated Security=True;Connect Timeout=30;Encrypt=False;Trust Server Certificate=False;Application Intent=ReadWrite;Multi Subnet Failover=False");
        }
    }

    // S — Single Responsibility Principle (Принцип единственной ответственности)
    // Employee — хранит только данные сотрудника (имя, табельный номер, тип оплаты).
    // Не занимается вычислениями, логикой UI или хранением состояния.

    public class Employee
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string EmployeeNumber { get; set; }
        public string PaymentType { get; set; } // SRP: Храним только нужные данные в БД
    }


    // O — Open/Closed Principle (Принцип открытости/закрытости)
    // Классы должны быть открыты для расширения, но закрыты для модификации.
    // OCP: Добавляя новые способы расчета, не изменяем существующие
    public interface IPaymentCalculator
    {

        //  I — Interface Segregation Principle (Принцип разделения интерфейсов)
        // Не заставляй клиента реализовывать интерфейс, который он не использует.
        // В данном контексте IPaymentCalculator — маленький, специализированный интерфейс с одним методом:
        decimal CalculateSalary();
    }

    public class SimplePieceworkPayment : IPaymentCalculator
    {
        private decimal hourlyRate;
        private decimal norm;
        private int units;

        public SimplePieceworkPayment(decimal hourlyRate, decimal norm, int units)
        {
            this.hourlyRate = hourlyRate;
            this.norm = norm;
            this.units = units;
        }

        public decimal CalculateSalary() => (hourlyRate / norm) * units;
    }

    public class PremiumPieceworkPayment : IPaymentCalculator
    {
        private decimal hourlyRate, norm, bonus;
        private int units;

        public PremiumPieceworkPayment(decimal hourlyRate, decimal norm, int units, decimal bonus)
        {
            this.hourlyRate = hourlyRate;
            this.norm = norm;
            this.units = units;
            this.bonus = bonus;
        }

        public decimal CalculateSalary() => (hourlyRate / norm) * units + bonus;
    }

    public class ProgressivePieceworkPayment : IPaymentCalculator
    {
        private int units;

        public ProgressivePieceworkPayment(int units)
        {
            this.units = units;
        }

        public decimal CalculateSalary()
        {
            int threshold = 110;
            decimal normalRate = 80m;
            decimal increasedRate = 85m;
            return units <= threshold
                ? units * normalRate
                : threshold * normalRate + (units - threshold) * increasedRate;
        }
    }

    public class IndirectPieceworkPayment : IPaymentCalculator
    {
        private decimal baseSalary, percent;

        public IndirectPieceworkPayment(decimal baseSalary, decimal percent)
        {
            this.baseSalary = baseSalary;
            this.percent = percent;
        }

        public decimal CalculateSalary() => baseSalary * (percent / 100);
    }
} // LSP, DIP: Все типы IPaymentCalculator можно использовать одинаково, не зная реализацию
