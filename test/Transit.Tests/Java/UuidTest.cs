// Copyright © 2014 Rick Beerendonk. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS-IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Beerendonk.Transit.Java;
using System;
using Xunit;

namespace Beerendonk.Transit.Tests.Java
{
    public class UuidTest
    {
        [Fact]
        public void TestConstructor()
        {
            var uuid = new Uuid(1, 2);

            Assert.Equal(1, uuid.MostSignificantBits);
            Assert.Equal(2, uuid.LeastSignificantBits);
        }

        [Fact]
        public void ShouldNotEqualNull()
        {
            var uuid = new Uuid(1, 2);

            Assert.False(uuid.Equals(null));
        }

        [Fact]
        public void ShouldNotEqualOtherType()
        {
            var uuid = new Uuid(1, 2);

            Assert.False(uuid.Equals(new object()));
        }

        [Fact]
        public void ShouldEqualSimilarUuidObject()
        {
            var uuid = new Uuid(1, 2);
            object other = new Uuid(1, 2);

            Assert.True(uuid.Equals(other));
        }

        [Fact]
        public void ShouldNotEqualOtherUuidObject()
        {
            var uuid = new Uuid(1, 2);
            object other = new Uuid(3, 4);

            Assert.False(uuid.Equals(other));
        }

        [Fact]
        public void ShouldEqualSimilarUuid()
        {
            var uuid = new Uuid(1, 2);

            Assert.True(uuid.Equals(new Uuid(1, 2)));
        }

        [Fact]
        public void ShouldNotEqualOtherUuid()
        {
            var uuid = new Uuid(1, 2);

            Assert.False(uuid.Equals(new Uuid(3, 4)));
        }

        [Fact]
        public void OperationOverloadEqual_ShouldEqualSimilarUuid()
        {
            var uuid = new Uuid(1, 2);

            Assert.True(uuid == new Uuid(1, 2));
        }

        [Fact]
        public void OperationOverloadEqual_ShouldNotEqualOtherUuid()
        {
            var uuid = new Uuid(1, 2);

            Assert.False(uuid == new Uuid(3, 4));
        }

        [Fact]
        public void OperationOverloadUnequal_ShouldUnequalOtherUuid()
        {
            var uuid = new Uuid(1, 2);

            Assert.True(uuid != new Uuid(3, 4));
        }

        [Fact]
        public void OperationOverloadUnequal_ShouldNotUnequalSimilarUuid()
        {
            var uuid = new Uuid(1, 2);

            Assert.False(uuid != new Uuid(1, 2));
        }

        [Fact]
        public void HashCodeShouldEqualGuidHashCode()
        {
            var guid = Guid.NewGuid();

            Assert.Equal(guid.GetHashCode(), ((Uuid)guid).GetHashCode());
        }

        [Fact]
        public void ConvertToGuidOfDefaultUuidShouldReturnDefaultGuid()
        {
            var uuid = default(Uuid);

            Assert.Equal(default(Guid), (Guid)uuid);
        }

        [Fact]
        public void ConvertToGuidShouldReturnCorrectGuid()
        {
            var uuid = new Uuid(-1714729031470661412L, -8577612382363445748L);

            Assert.Equal(new Guid("e8340f07-e924-40dc-88f6-32fc003c160c"), (Guid)uuid);
        }

        [Fact]
        public void ConvertToUuidOfDefaultGuidShouldReturnDefaultUuid()
        {
            var guid = default(Guid);

            Assert.Equal(default(Uuid), (Uuid)guid);
        }

        [Fact]
        public void ConvertToUuidShouldReturnCorrectUuid()
        {
            var guid = new Guid("e8340f07-e924-40dc-88f6-32fc003c160c");

            var uuid = (Uuid)guid; 

            Assert.Equal(-1714729031470661412L, uuid.MostSignificantBits);
            Assert.Equal(-8577612382363445748L, uuid.LeastSignificantBits);
        }

        [Fact]
        public void ToStringShouldReturnCorrectString()
        {
            var uuid = new Uuid(-1714729031470661412L, -8577612382363445748L);

            Assert.Equal("e8340f07-e924-40dc-88f6-32fc003c160c", uuid.ToString());
        }

        [Fact]
        public void FromStringShouldReturnCorrectUuid()
        {
            var uuid = Uuid.FromString("e8340f07-e924-40dc-88f6-32fc003c160c");

            Assert.Equal(-1714729031470661412L, uuid.MostSignificantBits);
            Assert.Equal(-8577612382363445748L, uuid.LeastSignificantBits);
        }
    }
}
