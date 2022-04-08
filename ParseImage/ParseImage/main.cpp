#include "LogRedirect.hpp"

#include <TermAPI.hpp>
#include <ParamsAPI2.hpp>
#include <env.hpp>
#include <envpath.hpp>
#include <fileio.hpp>

#include <opencv2/opencv.hpp>

using position = long long;
using length = long long;

// forward declare
template<std::integral T> struct basic_point;
template<std::integral T> struct basic_size;
template<std::integral T> struct basic_rectangle;

// 2D point type
template<std::integral T>
struct basic_point : std::pair<T, T> {
	using base = std::pair<T, T>;

	T& x;
	T& y;

	constexpr basic_point(const T& x, const T& y) : base(x, y), x{ this->first }, y{ this->second } {}

	basic_point<T>& operator=(const basic_point<T>& o)
	{
		x = o.x;
		y = o.y;
		return *this;
	}

	// conversion operator
	operator basic_size<T>() const { return{ x, y }; }
	operator cv::Point() const { return cv::Point(x, y); }
};
using Point = basic_point<position>;

// 2D size type
template<std::integral T>
struct basic_size : std::pair<T, T> {
	using base = std::pair<T, T>;

	T& width;
	T& height;

	constexpr basic_size(const T& x, const T& y) : base(x, y), width{ this->first }, height{ this->second } {}

	using base::operator=;

	basic_size(cv::Size sz) : base(sz.width, sz.height) {}

	basic_size<T>& operator=(const basic_size<T>& o)
	{
		width = o.width;
		height = o.height;
		return *this;
	}

	// conversion operator
	operator basic_point<T>() const { return{ width, height }; }
	operator cv::Size() const { return cv::Size(width, height); }
};
using Size = basic_size<position>;

template<std::integral T>
struct basic_rectangle : basic_point<T>, basic_size<T> {
	using basepoint = basic_point<T>;
	using basesize = basic_size<T>;

	basic_rectangle(const basepoint& pos, const basesize& size) : basepoint(pos), basesize(size) {}
	basic_rectangle(const T& x, const T& y, const T& width, const T& height) : basepoint(x, y), basesize(width, height) {}
	basic_rectangle(cv::Rect rect) : basepoint(rect.x, rect.y), basesize(rect.width, rect.height) {}

	operator cv::Rect() const { return cv::Rect{ static_cast<int>(this->x), static_cast<int>(this->y), static_cast<int>(this->width), static_cast<int>(this->height) }; }
};
using Rectangle = basic_rectangle<position>;

template<var::any_same<Point, Size, Rectangle> RetType>
RetType parse_string(const std::string& s, const std::string& seperators = ":,")
{
	const auto& [xstr, ystr] { str::split(s, seperators) };
	if (std::all_of(xstr.begin(), xstr.end(), isdigit) && std::all_of(ystr.begin(), ystr.end(), isdigit))
		return RetType{ str::stoll(xstr), str::stoll(ystr) };
	else throw make_exception("Cannot parse string '", s, "' into a valid pair of integrals!");
}

template<typename ImageType = cv::Mat>
struct Image {
	std::string filepath;
	ImageType image;

	Image(const std::string& path, const bool& load = true) : filepath{ path }, image{ load ? cv::imread(filepath) : ImageType{} } {}

	bool exists() const { return file::exists(filepath); }
	bool loaded() const { return !image.empty(); }

	void openDisplay() const
	{
		cv::namedWindow(filepath);

		cv::imshow(filepath, image);
	}
	void closeDisplay() const
	{
		cv::destroyWindow(filepath);
	}
};

using RGB = color::RGB<uchar>;

inline color::RGB<uchar> Vec3b_to_RGB(cv::Vec3b&& bgr) { return{ bgr[2], bgr[1], bgr[0] }; }

enum class Region : char {
	None,
	Solitude,			// Haafingar
	Morthal_Hold,		// Hjaalmarch
	Markarth,			// Reach
	Whiterun,			// Whiterun
	Falkreath,			// Falkreath
	Dawnstar,			// Pale
	Winterhold_Hold,	// Winterhold
	Windhelm,			// Eastmarch
	Riften,				// Rift

	Riverwood,			// Riverwood
	Winterhold,			// Winterhold City
	Helgen,				// Helgen
	Rorikstead,			// Rorikstead
	Morthal,			// Morthal City
};

inline std::ostream& operator<<(std::ostream& os, const Region& hc)
{
	switch (hc) {
	case Region::Solitude:
		os << "Haafingar";
		break;
	case Region::Morthal_Hold:
		os << "Hjaalmarch";
		break;
	case Region::Morthal:
		os << "Morthal";
		break;
	case Region::Markarth:
		os << "Reach";
		break;
	case Region::Whiterun:
		os << "Whiterun";
		break;
	case Region::Riverwood:
		os << "Riverwood";
		break;
	case Region::Rorikstead:
		os << "Rorikstead";
		break;
	case Region::Falkreath:
		os << "Falkreath";
		break;
	case Region::Dawnstar:
		os << "Pale";
		break;
	case Region::Winterhold:
		os << "Winterhold";
		break;
	case Region::Windhelm:
		os << "Eastmarch";
		break;
	case Region::Riften:
		os << "Rift";
		break;
	case Region::None: [[fallthrough]];
	default:break;
	}
	return os;
}

using ColorMap = std::map<RGB, Region>;
using HoldMap = std::vector<std::pair<Point, std::vector<Region>>>;

inline std::ostream& operator<<(std::ostream& os, const HoldMap& hm)
{
	os << "[HoldMap]\n";

	for (const auto& [pos, holds] : hm) {
		os << '(' << pos.first << ',' << pos.second << ")=[";
		for (auto it{ holds.begin() }; it != holds.end(); ++it) {
			os << '\"' << *it << "\"";
			if (std::distance(it, holds.end()) > 1)
				os << ",";
		}
		os << "]\n";
	}
	return os;
}

/// @brief	Translates index coordinates (origin 0,0 top-left) to cell coordinates (origin -74, 49 top-left)
constexpr Point offsetCellCoordinates(const Point& p, const Point& pMin = { 0, 0 }, const Point& pMax = { 149, 99 })
{
	const Point cellMin{ -74, 49 }, cellMax{ 75, -50 };

	const auto& translateAxis{ [](const auto& v, const auto& oldMin, const auto& oldMax, const auto& newMin, const auto& newMax) {
		if (oldMin == oldMax || newMin == newMax)
			throw make_exception("Invalid translation: ( ", oldMin, " - ", oldMax, " ) => ( ", newMin, " - ", newMax, " )");
		const auto
			& oldRange{ oldMax - oldMin },
			& newRange{ newMax - newMin };
		return (((v - oldMin) * newRange) / oldRange) + newMin;
	} };

	return{
		translateAxis(p.x, pMin.x, pMax.x, cellMin.x, cellMax.x),
		translateAxis(p.y, pMin.y, pMax.y, cellMin.y, cellMax.y)
	};
}

int main(const int argc, char** argv)
{
	// this corresponds to the colors used in cellmap.png to indicate 
	const ColorMap colormap{
		{ { 0xFF, 0x6D, 0x70 }, Region::Solitude },
		{ { 0x31, 0x70, 0x37 }, Region::Morthal_Hold },
		{ { 0x82, 0xD3, 0x7C }, Region::Markarth },
		{ { 0xFF, 0xD3, 0x7C }, Region::Whiterun },
		{ { 0xD8, 0xFF, 0x77 }, Region::Falkreath },
		{ { 0xB5, 0x7E, 0x9B }, Region::Dawnstar },
		{ { 0xBF, 0xD1, 0xBC }, Region::Winterhold_Hold },
		{ { 0x80, 0xB8, 0xCE }, Region::Windhelm },
		{ { 0xBC, 0x7A, 0xFF }, Region::Riften },

		{ { 0xFF, 0xB6, 0x7C }, Region::Riverwood },
		{ { 0xBF, 0xD1, 0xFF }, Region::Winterhold },
		{ { 0xBE, 0xFF, 0x77 }, Region::Helgen },
		{ { 0xC6, 0xFF, 0x7C }, Region::Rorikstead },
		{ { 0x31, 0xA0, 0x37 }, Region::Morthal },
	};
	try {
		opt::ParamsAPI2 args{ argc, argv, 'f', "file", 'd', "dim", 't', "timeout", 'o', "out" };
		env::PATH PATH;
		const auto& [myPath, myName] { PATH.resolve_split(argv[0]) };

		// show help
		if (args.empty() || args.check_any<opt::Flag, opt::Option>('h', "help")) {
			std::cout
				<< "ParseImage Usage:\n"
				<< "  " << std::filesystem::path(myName).replace_extension().generic_string() << " <OPTIONS>" << '\n'
				<< '\n'
				<< "OPTIONS:\n"
				<< "  -h  --help            Shows this usage guide.\n"
				<< "  -f  --file <PATH>     Specify an image to load.\n"
				<< "  -o  --out <PATH>      Specify a filepath to export the results to. Defaults to the name of the image\n"
				<< "                         file with the extension '.ini', in the current working directory.\n"
				<< "  -d  --dim <X:Y>       Specify the cell dimensions.\n"
				<< "      --display         Opens a window to display the loaded image file.\n"
				<< "  -t  --timeout <ms>    When '--display' is specified, closes the display window after '<ms>' milliseconds.\n"
				<< "                         a value of 0 will wait forever, which is the default behaviour.\n"
				<< std::endl;
		}



		if (const auto& fileArg{ args.typegetv_any<opt::Flag, opt::Option>('f', "file") }; fileArg.has_value()) {
			std::filesystem::path path{ fileArg.value() };

			if (!file::exists(path)) // if the path doesn't exist as-is, attempt to resolve it using the PATH variable.
				path = PATH.resolve(path, { (path.has_extension() ? path.extension().generic_string() : ""), ".png", ".jpg", ".bmp" });

			int windowTimeout{ args.castgetv_any<int, opt::Flag, opt::Option>(str::stoi, 't', "timeout").value_or(0) };

			if (file::exists(path)) {
				LogRedirect streams;
				streams.redirect(StandardStream::STDOUT | StandardStream::STDERR, "OpenCV.log");


				if (Image img{ path.generic_string() }; img.loaded()) {
					std::clog << "Successfully loaded image file '" << path << '\'' << std::endl;

					if (const auto& dimArg{ args.typegetv_any<opt::Flag, opt::Option>('d', "dim") }; dimArg.has_value()) {
						Size partSize = parse_string<Size>(dimArg.value(), ":,");
						std::clog << "Partition Size:  [ " << partSize.width << " x " << partSize.height << " ]\n";

						const length& cols{ img.image.cols / partSize.width };
						const length& rows{ img.image.rows / partSize.height };

						bool display_each{ args.checkopt("display") };
						const std::string windowName{ "Display" };

						if (display_each)
							cv::namedWindow(windowName); // open a window

						HoldMap vec;
						vec.reserve(cols * rows);

						const auto& identify_colors{ [&colormap](cv::Mat&& part) {
							std::vector<Region> detected;
							detected.reserve(colormap.size());

							const auto& channels{ part.channels() };
							if (channels != 3)
								throw make_exception("This program requires an image with at least 3 channels!");
							auto rows{ part.rows }, cols{ part.cols };

							for (int y{ 0 }; y < rows; ++y) {
								for (int x{ 0 }; x < cols; ++x) {
									const Point& pos{ x, y };
									if (RGB color{ Vec3b_to_RGB(std::move(part.at<cv::Vec3b>(pos))) }; colormap.contains(color)) {
										if (Region det{ colormap.at(color) }; !std::any_of(detected.begin(), detected.end(), [&det](auto&& hc) -> bool { return det == hc; })) {
											detected.emplace_back(det);
											std::clog << "  + " << det << '\n';
										}
									}
								}
							}

							detected.shrink_to_fit();
							return detected;
						} };

						size_t i = 0;
						for (length y{ 0 }; y < rows; ++y) {
							for (length x{ 0 }; x < cols; ++x, ++i) {
								const auto& rect{ Rectangle(x * partSize.width, y * partSize.height, partSize.width, partSize.height) };
								auto part{ img.image(rect) };
								const auto& cellPos{ offsetCellCoordinates(Point{ x, y }) };
								std::clog << "Processing Partition #" << i << '\n'
									<< "  Partition Index:   ( " << x << ", " << y << " )\n"
									<< "  Cell Coordinates:  ( " << cellPos.x << ", " << cellPos.y << ")\n";
								if (display_each) {
									cv::imshow(windowName, part); // display the image in the window
									cv::waitKey(windowTimeout);
								}
								if (auto results{ identify_colors(std::move(part)) }; !results.empty()) {
									vec.emplace_back(std::make_pair(cellPos, std::move(results)));
								}
							}
						}

						std::clog << "Finished processing image partitions." << std::endl;
						std::clog << "Found " << vec.size() << " partitions with valid color map data." << std::endl;

						vec.shrink_to_fit();

						std::filesystem::path outpath{ path.filename() };
						outpath.replace_extension(".ini");
						if (const auto& outArg{ args.typegetv_any<opt::Flag, opt::Option>('o', "out") }; outArg.has_value())
							outpath = outArg.value(); // override the output path

						if (file::write(outpath, vec)) {
							std::clog << "Successfully saved the lookup matrix to " << outpath << std::endl;
						}
						else throw make_exception("Failed to write to output file ", outpath, "!");

						if (display_each)
							cv::destroyWindow(windowName);
					}
					else if (args.checkopt("display")) {
						std::clog << "Opening display..." << std::endl;
						img.openDisplay();
						std::clog << "Press any key when the window is open to exit." << std::endl;
						cv::waitKey(windowTimeout);
						img.closeDisplay();
					}
					else throw make_exception("No arguments were included that specify what to do with the image! ('-d'/'--dim', '--display')");
				}
				else throw make_exception("Failed to load image file '", path, '\'');

				streams.reset(StandardStream::ALL);
			}
			else throw make_exception("Failed to resolve filepath ", path, "! (File doesn't exist)");
		}
		else throw make_exception("Nothing to do! (No filepath was specified with '-f'/'--file')");

		return 0;
	} catch (const std::exception& ex) {
		std::cerr << term::get_error() << ex.what() << std::endl;
		return 1;
	} catch (...) {
		std::cerr << term::get_crit() << "An unknown exception occurred!" << std::endl;
		return 1;
	}
}
