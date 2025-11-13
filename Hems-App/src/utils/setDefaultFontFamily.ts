import { Text, TextInput } from 'react-native';
import { FONTS } from '../theme/constants';

/**
 * Set default font family for all Text and TextInput components globally
 * This should be called once at app startup
 */
export const setDefaultFontFamily = () => {
  // Override default Text component props
  const TextRender = Text.render;
  const initialDefaultProps = Text.defaultProps;
  Text.defaultProps = {
    ...initialDefaultProps,
    style: [{ fontFamily: FONTS.regular }, initialDefaultProps?.style],
  };
  Text.render = function render(props: any) {
    return TextRender.call(this, {
      ...props,
      style: [{ fontFamily: FONTS.regular }, props.style],
    });
  };

  // Override default TextInput component props
  const TextInputRender = TextInput.render;
  const initialTextInputDefaultProps = TextInput.defaultProps;
  TextInput.defaultProps = {
    ...initialTextInputDefaultProps,
    style: [{ fontFamily: FONTS.regular }, initialTextInputDefaultProps?.style],
  };
  TextInput.render = function render(props: any) {
    return TextInputRender.call(this, {
      ...props,
      style: [{ fontFamily: FONTS.regular }, props.style],
    });
  };
};
