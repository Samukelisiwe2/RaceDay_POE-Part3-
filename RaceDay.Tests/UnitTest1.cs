using RaceDay.Models;
using Xunit;

namespace RaceDay.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void NewEnrollment_ShouldHavePendingStatus()
        {
            // Arrange
            var enrollment = new Enrollment();

            // Act
            string status = enrollment.EnrollmentStatus;

            // Assert
            Assert.Equal("Pending", status);
        }

        [Fact]
        public void NewEnrollment_ShouldHaveTodaysDate()
        {
            // Arrange
            var enrollment = new Enrollment();

            // Assert
            Assert.Equal(DateTime.Today, enrollment.EnrollmentDate.Date);
        }
    }
}