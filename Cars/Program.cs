using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;

namespace Cars
{
    class Program
    {

        // Comment to test committing
        static void Main(string[] args)
        {
            Database.SetInitializer(new DropCreateDatabaseIfModelChanges<CarDb>());
            //InsertData();
            QueryData();
        }

        private static void QueryData()
        {
            var db = new CarDb();

            var query =
                from car in db.Cars
                orderby car.Combined descending, car.Name ascending
                select car;

            foreach (var car in query.Take(10))

            {
                Console.WriteLine($"{car.Name} : {car.Combined}");
            }
        }

        private static void InsertData()
        {
            var cars = ProcessFile("fuel.csv");
            var db = new CarDb();

            if (!db.Cars.Any())
            {
                foreach (var car in cars)
                {
                    db.Cars.Add(car);
                }
                db.SaveChanges();
            }
        }

        private static void RunQueries()
        {
            var cars = ProcessFile("fuel.csv");
            var manufacturers = ProcessManufacturers("manufacturers.csv");

            //var query0 = from car in cars
            //             orderby car.Combined descending, car.Name
            //             select car;

            var query1 = from car in cars
                         join manufacturer in manufacturers
                            on car.Manufacturer equals manufacturer.Name
                         orderby car.Combined descending, car.Name
                         select new
                         {
                             manufacturer.Headquarters,
                             car.Name,
                             car.Combined
                         };

            var query2 = cars.Join(manufacturers,
                                    c => c.Manufacturer,
                                    m => m.Name,
                                    (c, m) => new
                                    {
                                        m.Headquarters,
                                        c.Name,
                                        c.Combined
                                    }).OrderByDescending(c => c.Combined).ThenBy(c => c.Name);

            // use manufacturer and year as a composite key
            var query3 = from car in cars
                         join manufacturer in manufacturers
                            on new { car.Manufacturer, car.Year }
                            equals new { Manufacturer = manufacturer.Name, manufacturer.Year } // fields need to have the same name
                         orderby car.Combined descending, car.Name
                         select new
                         {
                             manufacturer.Headquarters,
                             car.Name,
                             car.Combined
                         };

            var query4 = cars.Join(manufacturers,
                                    c => new { c.Manufacturer, c.Year },
                                    m => new { Manufacturer = m.Name, m.Year },
                                    (c, m) => new
                                    {
                                        m.Headquarters,
                                        c.Name,
                                        c.Combined
                                    }).OrderByDescending(c => c.Combined).ThenBy(c => c.Name);

            //foreach (var car in query4.Take(10))
            //{
            //    Console.WriteLine($"{car.Headquarters} : {car.Name} : {car.Combined}");
            //}

            // for each manufacturer, show the two most fuel-efficient cars
            var query5 =
                from car in cars
                group car by car.Manufacturer.ToUpper() into manufacturer
                orderby manufacturer.Key
                select manufacturer;

            var query6 =
                cars.GroupBy(c => c.Manufacturer.ToUpper())
                    .OrderBy(g => g.Key);

            //foreach (var manufacturer in query6)
            //{
            //    Console.WriteLine(manufacturer.Key);
            //    foreach (var car in manufacturer.OrderByDescending(c => c.Combined).Take(2))
            //    {
            //        Console.WriteLine($"\t{car.Name} : {car.Combined}");
            //    }
            //}

            // for each manufacturer, show its origin country and its two most fuel-efficient cars
            var query7 =
                from manufacturer in manufacturers // have access to all the manufacturers and their associated group of cars (carGroup)
                join car in cars on manufacturer.Name equals car.Manufacturer
                    into carGroup
                orderby manufacturer.Name
                select new
                {
                    Manufacturer = manufacturer,
                    Cars = carGroup
                };



            foreach (var group in query7)
            {
                Console.WriteLine($"{group.Manufacturer.Name} : {group.Manufacturer.Headquarters}");
                foreach (var car in group.Cars.OrderByDescending(c => c.Combined).Take(2))
                {
                    Console.WriteLine($"\t{car.Name} : {car.Combined}");
                }
            }

            var query8 =
                from car in cars
                group car by car.Manufacturer into carGroup
                select new
                {
                    Name = carGroup.Key,
                    Max = carGroup.Max(c => c.Combined),
                    Min = carGroup.Min(c => c.Combined),
                    Avg = carGroup.Average(c => c.Combined)
                } into result
                orderby result.Max descending
                select result;

            foreach (var result in query8)
            {
                Console.WriteLine($"{result.Name}");
                Console.WriteLine($"\t Max: {result.Max}");
                Console.WriteLine($"\t Min: {result.Min}");
                Console.WriteLine($"\t Avg: {result.Avg}");
            }
        }

        private static List<Manufacturer> ProcessManufacturers(string path)
        {
            return File.ReadAllLines(path).Where(line => line.Length > 1).ToManufacturer().ToList();
        }

        private static List<Car> ProcessFile(string path)
        {
            return File.ReadAllLines(path).Skip(1).Where(line => line.Length > 1).ToCar().ToList();
        }




    }

    public static class ManufacturerExtensions
    {
        public static IEnumerable<Manufacturer> ToManufacturer(this IEnumerable<string> source)
        {
            foreach (var line in source)
            {
                var columns = line.Split(',');
                yield return new Manufacturer
                {
                    Name = columns[0],
                    Headquarters = columns[1],
                    Year = int.Parse(columns[2])
                };
            }
        }
    }

    public static class CarExtensions
    {
        public static IEnumerable<Car> ToCar(this IEnumerable<string> source)
        {
            foreach (var line in source)
            {
                var columns = line.Split(',');
                yield return new Car
                {
                    Year = int.Parse(columns[0]),
                    Manufacturer = columns[1],
                    Name = columns[2],
                    Displacement = double.Parse(columns[3]),
                    Cylinders = int.Parse(columns[4]),
                    City = int.Parse(columns[5]),
                    Highway = int.Parse(columns[6]),
                    Combined = int.Parse(columns[7]),
                };
            }
        }

    }
}
