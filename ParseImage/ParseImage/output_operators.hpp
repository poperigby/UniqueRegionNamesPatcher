#pragma once
#include "types.hpp"

#include <strmath.hpp>

inline std::ostream& operator<<(std::ostream& os, const std::vector<Region>& regionList)
{
	os << "[ ";
	for (auto it{ regionList.begin() }, endit{ regionList.end() }; it != endit; ++it) {
		os << '"' << *it << '"';
		if (std::distance(it, regionList.end()) > 1ull)
			os << ", ";
	}
	return os << " ]";
}

inline std::ostream& operator<<(std::ostream& os, const RGB& rgb)
{
	for (int i{ 0 }; i < rgb.channel_count(); ++i) {

	}

	std::string n{ str::fromBase10(rgb.r(), 16) };
	if (n.size() < 2ull)
		os << '0';
	os << n;
	n = str::fromBase10(rgb.g(), 16);
	str::fromBase10(rgb.b(), 16);
	return os << '#' << str::stringify(std::hex, static_cast<short>(rgb.r()), static_cast<short>(rgb.g()), static_cast<short>(rgb.b())) << '\n';
}

// file writing operators:
inline std::ostream& operator<<(std::ostream& os, const HoldMap& holdmap)
{
	for (const auto& [pos, regions] : holdmap)
		os << '(' << pos.first << ',' << pos.second << ") = " << regions << '\n';
	return os;
}
