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

	T& x{ this->first };
	T& y{ this->second };

	using base::base; // inherit constructors

	basic_point(cv::Point p) : base(p.x, p.y) {}

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

	T& width{ this->first };
	T& height{ this->second };

	using base::base; // inherit constructors
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

enum class HoldCapital : char {
	None,
	Solitude,
	Morthal,
	Markarth,
	Whiterun,
	Falkreath,
	Dawnstar,
	Winterhold,
	Windhelm,
	Riften,
};

int main(const int argc, char** argv)
{
	// this corresponds to the colors used in cellmap.png to indicate 
	const std::map<color::RGB<unsigned char>, HoldCapital> colormap{
		{ { 0xFF, 0x6D, 0x70 }, HoldCapital::Solitude },
		{ { 0x31, 0x70, 0x37 }, HoldCapital::Morthal },
		{ { 0x82, 0xD3, 0x7C }, HoldCapital::Markarth },
		{ { 0xFF, 0xD3, 0x7C }, HoldCapital::Whiterun },
		{ { 0xD8, 0xFF, 0x77 }, HoldCapital::Falkreath },
		{ { 0xB5, 0x7E, 0x9B }, HoldCapital::Dawnstar },
		{ { 0xBF, 0xD1, 0xBC }, HoldCapital::Winterhold },
		{ { 0x80, 0xB8, 0xCE }, HoldCapital::Windhelm },
		{ { 0xBC, 0x7A, 0xFF }, HoldCapital::Riften },
	};
	try {
		opt::ParamsAPI2 args{ argc, argv, 'f', "file", 'd', "dim", 't', "timeout" };
		env::PATH PATH;
		const auto& [myPath, myName] { PATH.resolve_split(argv[0]) };

		// show help
		if (args.empty() || args.check_any<opt::Flag, opt::Option>('h', "help")) {
			std::cout
				<< "ParseImage Usage:\n"
				<< "  " << std::filesystem::path(myName).replace_extension().generic_string() << " <OPTIONS>" << '\n'
				<< '\n'
				<< "OPTIONS:\n"
				<< "  -h  --help            Shows this usage guide." << '\n'
				<< "  -f  --file <PATH>     Specify an image to load." << '\n'
				<< "  -d  --dim <X:Y>       Specify the cell dimensions." << '\n'
				<< "      --display         Opens a window to display the loaded image file." << '\n'
				<< "  -t  --timeout <ms>    When '--display' is specified, closes the display window after '<ms>' milliseconds.\n"
				<< "                         a value of 0 will wait forever, which is the default behaviour."
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

						cv::Mat partition{ img.image.size(), img.image.type(), cv::Scalar::all };

						bool display_each{ args.checkopt("display") };
						const std::string windowName{ "Display" };

						long long i = 0;

						if (display_each)
							cv::namedWindow(windowName); // open a window

						for (length y{ 0 }; y < rows; ++y) {
							for (length x{ 0 }; x < cols; ++x, ++i) {
								const auto& rect{ Rectangle(x * partSize.width, y * partSize.height, partSize.width, partSize.height) };
								const auto& part{ img.image(rect) };
								if (display_each) {
									std::clog << "Displaying Partition #" << i << "\n  Location: ( " << x << ", " << y << " )\n";
									cv::imshow(windowName, part); // display the image in the window
									cv::waitKey(windowTimeout);
								}
							}
						}

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
